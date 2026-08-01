using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.Domain.Events.Payments;
using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

public class PaymentProcessedHandler(IInboxProcessor inbox, ILogger<PaymentProcessedHandler> logger)
{
    public Task Handle(PaymentProcessedIntegrationEvent message, CancellationToken ct) =>
        inbox.ProcessAsync(message, "ConsumerA", () =>
        {
            logger.LogInformation(
                "[ConsumerA] Payment processed — PaymentId: {PaymentId}, OrderId: {OrderId}, Amount: {Amount}, Status: {Status}",
                message.PaymentId, message.OrderId, message.Amount, message.Status);
            return Task.CompletedTask;
        }, ct);
}
