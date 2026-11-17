using GM.Messaging.Sample.ConsumerA.Worker.Handlers;
using GM.Messaging.Sample.ConsumerA.Infrastructure;
using GM.Messaging.Sample.ConsumerA.Persistence;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Handlers take ConsumerDbContext as well as IInboxProcessor; AddGMInbox (in AddPersistence) tells
// Wolverine to resolve it from the message's scope, so both share one instance and a handler's changes
// commit with the inbox row.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);

// The sample's stand-in for deploying a fix: once switched on, ConsumerA books orders in XXX, so replaying the
// dead-lettered one succeeds (the recovery CI walks through).
builder.Services.AddSingleton<CurrencyPolicy>();

var app = builder.Build();

app.MapInfrastructure();
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
