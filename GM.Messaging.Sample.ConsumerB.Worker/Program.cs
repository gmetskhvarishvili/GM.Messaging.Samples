using GM.Messaging.Persistence;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;
using GM.Messaging.Sample.ConsumerB.Infrastructure;
using GM.Messaging.Sample.ConsumerB.Persistence;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;
using GM.Messaging.Sample.ConsumerB.Worker.InboxHandlers;
using GM.Messaging.Sample.Domain.Events.Users;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Handlers take ConsumerDbContext as well as IInboxProcessor. Wolverine can't build a DbContext in its
// generated code, so it takes it from the message's scope, as GM.Messaging does for IInboxProcessor:
// both then share one instance, and a handler's changes commit with the inbox row.
builder.Services.AddInfrastructure(builder.Configuration, wolverine =>
    wolverine.CodeGeneration.AlwaysUseServiceLocationFor<ConsumerDbContext>());
builder.Services.AddPersistence(builder.Configuration);

// GM.Messaging's inbox worker handles the events UserRegisteredHandler ingests: welcome emails are sent
// off the broker's delivery, retried with backoff and dead-lettered (Messaging:Inbox settings).
builder.Services.AddGMInboxWorker<InboxMessage, ConsumerDbContext>();
builder.Services.AddGMInboxHandler<UserRegisteredIntegrationEvent, SendWelcomeEmail>();

var app = builder.Build();

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
