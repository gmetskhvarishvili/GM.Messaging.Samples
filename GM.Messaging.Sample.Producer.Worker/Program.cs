using GM.Messaging.Persistence;
using GM.Messaging.Sample.Domain.Events;
using GM.Messaging.Sample.Domain.Events.Inventory;
using GM.Messaging.Sample.Producer.Infrastructure;
using GM.Messaging.Sample.Persistence;
using GM.Messaging.Sample.Persistence.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// The upcaster for InventoryUpdated v1 → v2, so the relay can still publish rows written before the change.
builder.Services.AddGMIntegrationEvents(events => events
    .AddAssembly(typeof(SampleEventVersions).Assembly)
    .AddUpcaster<InventoryUpdatedIntegrationEvent>(fromVersion: 1, SampleEventVersions.InventoryUpdatedV1ToV2));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration, runOutboxRelay: true);

var app = builder.Build();

app.MapInfrastructure();

// The API owns the messaging_api database and applies its migrations. Wait for them instead of racing it:
// two processes migrating one database at once can both try to create the same tables.
await WaitForMigrationsAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);

await app.RunAsync();

static async Task WaitForMigrationsAsync(IServiceProvider services, ILogger logger, CancellationToken cancellationToken)
{
    var deadline = DateTime.UtcNow.AddMinutes(5);
    while (true)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!(await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                return;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The database doesn't exist yet.
            logger.LogDebug(ex, "The application database isn't reachable yet.");
        }

        if (DateTime.UtcNow > deadline)
            throw new InvalidOperationException("The API didn't apply the application database's migrations within 5 minutes.");

        logger.LogInformation("Waiting for the API to apply the application database's migrations.");
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}
