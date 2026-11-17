using GM.API.Startup;
using GM.HealthChecks.EntityFramework;
using GM.HealthChecks.Messaging;
using GM.Messaging.Sample.Common.Admin;
using GM.Messaging.Sample.Common.KeyManagement;
using GM.Messaging.Sample.Common.Telemetry;
using OpenTelemetry.Trace;
using GM.Messaging.Sample.Persistence;
using GM.Messaging.Sample.Persistence.Context;
using Microsoft.EntityFrameworkCore;

var builder = ProgramExtension.CreateGMBuilder(args);

builder.Services.ConfigureGMServices(
    builder.Configuration,
    "policyName",
    "SwaggerDocOptions");

builder.Services.AddPersistence(builder.Configuration);

// GM.API maps /health/live, /health/ready and /health/startup. Readiness also checks the database, and
// the outbox backlog (pending messages, the oldest one's age, dead letters) reports Degraded when the
// relay falls behind or gives up on messages.
builder.Services.AddHealthChecks()
    .AddGMDatabaseCheck<ApplicationDbContext>()
    .AddGMMessagingBacklogCheck(backlog => backlog.MaxPendingAge = TimeSpan.FromMinutes(2));

// The outbox admin endpoints (GM.Messaging.AspNetCore) under /admin/messaging/outbox, behind the
// messaging-admin policy (an X-Admin-Key header in this sample).
builder.Services.AddSampleMessagingAdmin(builder.Configuration);

// The request's trace continues through the outbox: the relay publishes each event in it.
builder.Services.AddSampleTelemetry(builder.Configuration, tracing => tracing.AddAspNetCoreInstrumentation());

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseGMServices();
app.MapSampleMessagingAdmin();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (context != null)
        {
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
                await context.Database.MigrateAsync();

            // The shared per-person key database: the API owns it too.
            var keys = scope.ServiceProvider.GetRequiredService<SubjectKeysDbContext>();
            if ((await keys.Database.GetPendingMigrationsAsync()).Any())
                await keys.Database.MigrateAsync();

            var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContextSeed>>();
            if (logger != null)
                await ApplicationDbContextSeed.SeedAsync(context, logger);
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
    }
}

await app.RunAsync();

