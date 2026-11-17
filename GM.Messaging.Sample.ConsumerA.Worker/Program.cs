using GM.Messaging.Sample.ConsumerA.Worker.Handlers;
using GM.Messaging.AspNetCore;
using GM.HealthChecks;
using GM.HealthChecks.EntityFramework;
using GM.HealthChecks.Messaging;
using GM.Messaging.Sample.ConsumerA.Infrastructure;
using GM.Messaging.Sample.ConsumerA.Persistence;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Handlers take IConsumerUnitOfWork as well as IInboxProcessor. AddGMInbox (in AddPersistence) tells Wolverine to
// resolve ConsumerDbContext from the message's scope, so the unit of work's repositories and the inbox share one
// instance and a handler's changes commit with the inbox row.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

// /health/live, /health/ready (database, RabbitMQ, the inbox backlog) and /health/startup.
builder.Services.AddGMHealthChecks()
    .AddGMDatabaseCheck<ConsumerDbContext>()
    .AddGMMessagingCheck()
    .AddGMMessagingBacklogCheck(backlog => backlog.MaxPendingAge = TimeSpan.FromMinutes(2))
    // What waits on RabbitMQ: consumer queues near their limit, dead letters, unrouted messages (GM.Messaging's queue monitor).
    .AddGMBrokerQueueCheck();

// The sample's stand-in for deploying a fix: once switched on, ConsumerA books orders in XXX, so replaying the
// dead-lettered one succeeds (the recovery CI walks through).
builder.Services.AddSingleton<CurrencyPolicy>();

var app = builder.Build();

app.MapGMHealthChecks();
// GM.Messaging's admin endpoints under /admin/messaging (whatever this host has: outbox, inbox, broker queues,
// subject keys). The sample has no login, so they're open; a real service passes its own policy:
// admin.AuthorizationPolicy = "your-policy". Broker message bodies show with [SensitiveData] masked.
app.MapGMMessagingAdmin(admin =>
{
    admin.AllowAnonymous = true;
    admin.MessageBodies = BrokerMessageBodies.Redacted;
});
app.MapPost("/admin/sample/accept-failing-currency", (CurrencyPolicy currencies) =>
    {
        currencies.AcceptFailingCurrency();
        return Results.NoContent();
    });

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetService<ConsumerDbContext>();
        if (context != null)
        {
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
                await context.Database.MigrateAsync();

            var logger = scope.ServiceProvider.GetService<ILogger<ConsumerDbContextSeed>>();
            if (logger != null)
                await ConsumerDbContextSeed.SeedAsync(context, logger);
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
    }
}

await app.RunAsync();
