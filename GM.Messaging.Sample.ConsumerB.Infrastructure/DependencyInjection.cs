using GM.Messaging.Sample.Common.KeyManagement;
using GM.Messaging.Persistence.Protection;
using GM.Messaging.Sample.Common.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace GM.Messaging.Sample.ConsumerB.Infrastructure;

public static class DependencyInjection
{
    /// <summary>GM.Messaging (Wolverine + RabbitMQ from configuration) and OpenTelemetry.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<WolverineOptions>? configureWolverine = null)
    {
        services.AddGMMessaging(configuration, configureWolverine);
        // Decrypts [SensitiveData] values (the key ring in Messaging:SensitiveData, shared with the producer).
        services.AddGMSensitiveData(configuration);
        // Per-person keys (UserRegistered, ReturnRequested) in the shared messaging_keys database, and Data Protection
        // for the dp: keys in the key ring.
        services.AddSampleKeyManagement(configuration);
        services.AddSampleTelemetry(configuration);

        return services;
    }
}
