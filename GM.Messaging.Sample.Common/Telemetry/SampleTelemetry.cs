using GM.Messaging.Persistence.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GM.Messaging.Sample.Common.Telemetry;

public static class SampleTelemetry
{
    /// <summary>
    /// Traces and metrics for GM.Messaging (the outbox relay, the inbox, the inbox worker) and Wolverine,
    /// exported over OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set (docker-compose points it at
    /// Jaeger). A request, the event it raised, the relay's publish and each consumer's handling then
    /// show up as one trace.
    /// </summary>
    public static IServiceCollection AddSampleTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<TracerProviderBuilder>? tracing = null)
    {
        var serviceName = configuration["Messaging:ServiceName"] ?? configuration["OTEL_SERVICE_NAME"] ?? "gm-messaging-sample";
        var export = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(builder =>
            {
                builder.AddSource(MessagingTelemetry.Name).AddSource("Wolverine");
                tracing?.Invoke(builder);
                if (export)
                    builder.AddOtlpExporter();
            })
            .WithMetrics(builder =>
            {
                builder.AddMeter(MessagingTelemetry.Name).AddMeter("Wolverine");
                if (export)
                    builder.AddOtlpExporter();
            });

        return services;
    }
}
