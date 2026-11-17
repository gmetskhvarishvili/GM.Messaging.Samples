using GM.Messaging.AspNetCore;
using GM.HealthChecks;
using GM.HealthChecks.EntityFramework;
using GM.HealthChecks.Messaging;
using GM.Messaging.Persistence;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;
using GM.Messaging.Sample.ConsumerB.Infrastructure;
using GM.Messaging.Sample.ConsumerB.Persistence;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;
using GM.Messaging.Sample.ConsumerB.Worker.InboxHandlers;
using GM.Messaging.Sample.Domain.Events;
using GM.Messaging.Sample.Domain.Events.Inventory;
using GM.Messaging.Sample.Domain.Events.Users;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// The upcaster for InventoryUpdated v1 → v2: a producer still on v1 sends the old shape with
// gm-event-version: 1, and this consumer reads it as v2.
builder.Services.AddGMIntegrationEvents(events => events
    .AddAssembly(typeof(SampleEventVersions).Assembly)
    .AddUpcaster<InventoryUpdatedIntegrationEvent>(fromVersion: 1, SampleEventVersions.InventoryUpdatedV1ToV2));

// Handlers take IConsumerUnitOfWork as well as IInboxProcessor. AddGMInbox (in AddPersistence) tells Wolverine to
// resolve ConsumerDbContext from the message's scope, so the unit of work's repositories and the inbox share one
// instance and a handler's changes commit with the inbox row.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

// GM.Messaging's inbox worker handles the events UserRegisteredHandler ingests: welcome emails are sent
// off the broker's delivery, retried with backoff and dead-lettered (Messaging:Inbox settings).
builder.Services.AddGMInboxWorker<InboxMessage, ConsumerDbContext>();
builder.Services.AddGMInboxHandler<UserRegisteredIntegrationEvent, SendWelcomeEmail>();

// /health/live, /health/ready (database, RabbitMQ, the inbox backlog) and /health/startup. The sample
// dead-letters one welcome email on purpose, so dead letters don't count against readiness here.
builder.Services.AddGMHealthChecks()
    .AddGMDatabaseCheck<ConsumerDbContext>()
    .AddGMMessagingCheck()
    .AddGMMessagingBacklogCheck(backlog =>
    {
        backlog.MaxPendingAge = TimeSpan.FromMinutes(2);
        backlog.MaxDeadLettered = 10;
    })
    // What waits on RabbitMQ: consumer queues near their limit, dead letters, unrouted messages (GM.Messaging's queue monitor).
    .AddGMBrokerQueueCheck();

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
