using GM.Messaging.Broker;
using GM.Messaging.Persistence.Administration;

namespace GM.Messaging.Sample.ConsumerB.Worker.Admin;

/// <summary>
/// Operations endpoints for this consumer: its inbox (GM.Messaging's <see cref="IInboxAdministration"/>)
/// and the broker's queues (<see cref="IBrokerQueueAdministration"/>): the shared dead-letter queue
/// Wolverine moves messages to after their last retry, and the unrouted queue that keeps messages
/// published to an exchange with no matching queue. In a real service, protect these endpoints.
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints, IConfiguration configuration)
    {
        var deadLetterQueue = configuration["ConsumerTopology:DeadLetterQueue"] ?? "gm.dead-letters";
        // The unrouted queue has the alternate exchange's name (Messaging:RabbitMq:UnroutedExchange).
        var unroutedQueue = configuration["Messaging:RabbitMq:UnroutedExchange"] is { Length: > 0 } name ? name : "gm.unrouted";
        var admin = endpoints.MapGroup("/admin");

        // The inbox: events ingested for the inbox worker, and the ones it gave up on.
        var inbox = admin.MapGroup("/inbox");
        inbox.MapGet("/", (IInboxAdministration administration, CancellationToken ct) => administration.GetBacklogAsync(ct));
        inbox.MapGet("/dead-letters", (IInboxAdministration administration, int? skip, int? take, CancellationToken ct) =>
            administration.GetDeadLetteredAsync(skip ?? 0, take ?? 50, ct));
        inbox.MapPost("/dead-letters/{consumerName}/{eventId:guid}/requeue", async (string consumerName, Guid eventId, IInboxAdministration administration, CancellationToken ct) =>
            await administration.RequeueAsync(eventId, consumerName, ct) ? Results.NoContent() : Results.NotFound());
        inbox.MapPost("/dead-letters/requeue", (IInboxAdministration administration, CancellationToken ct) => administration.RequeueAllDeadLetteredAsync(ct));

        // The broker's dead-letter queue: count, look without consuming, and replay to each message's
        // original queue once the cause is fixed.
        var deadLetters = admin.MapGroup("/broker/dead-letters");
        deadLetters.MapGet("/", async (IBrokerQueueAdministration broker, CancellationToken ct) =>
            new QueueCount(deadLetterQueue, await broker.CountAsync(deadLetterQueue, ct)));
        deadLetters.MapGet("/messages", (IBrokerQueueAdministration broker, int? max, CancellationToken ct) =>
            broker.PeekAsync(deadLetterQueue, max ?? 20, ct));
        deadLetters.MapPost("/replay", (IBrokerQueueAdministration broker, int? max, CancellationToken ct) =>
            broker.ReplayDeadLettersAsync(deadLetterQueue, max ?? 100, ct));

        // Unrouted messages: published before any queue was bound for them. Once the consumer's queue
        // exists, move them back to their exchange.
        var unrouted = admin.MapGroup("/broker/unrouted");
        unrouted.MapGet("/", async (IBrokerQueueAdministration broker, CancellationToken ct) =>
            new QueueCount(unroutedQueue, await broker.CountAsync(unroutedQueue, ct)));
        unrouted.MapGet("/messages", (IBrokerQueueAdministration broker, int? max, CancellationToken ct) =>
            broker.PeekAsync(unroutedQueue, max ?? 20, ct));
        unrouted.MapPost("/move", (IBrokerQueueAdministration broker, string exchange, int? max, CancellationToken ct) =>
            broker.MoveAsync(unroutedQueue, exchange, max ?? 100, ct));

        return endpoints;
    }
}

/// <summary>How many messages a broker queue holds.</summary>
public sealed record QueueCount(string Queue, uint Messages);
