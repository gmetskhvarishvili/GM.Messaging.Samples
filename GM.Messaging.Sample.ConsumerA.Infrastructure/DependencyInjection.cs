using GM.Messaging.Persistence.Protection;
using GM.Messaging.Sample.Common.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace GM.Messaging.Sample.ConsumerA.Infrastructure;

public static class DependencyInjection
{
    /// <summary>GM.Messaging (Wolverine + RabbitMQ from configuration) and OpenTelemetry.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<WolverineOptions>? configureWolverine = null)
    {
        services.AddGMMessaging(configuration, configureWolverine);
        // Decrypts [SensitiveData] values: the key ring (Messaging:SensitiveData, shared with the producer) and the
        // per-person keys in the shared messaging_keys database.
        services.AddGMSensitiveData(configuration, subjectKeys: (db, connectionString) =>
            db.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
        services.AddSampleTelemetry(configuration);

        return services;
    }
}
