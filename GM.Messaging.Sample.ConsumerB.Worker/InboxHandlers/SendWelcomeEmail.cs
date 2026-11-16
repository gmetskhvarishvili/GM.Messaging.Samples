using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;
using GM.Messaging.Sample.Domain.Events.Users;
using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.ConsumerB.Worker.InboxHandlers;

/// <summary>
/// Called by GM.Messaging's inbox worker for each ingested UserRegistered event. The recorded email and
/// the inbox row commit together. Addresses at the bounce domain fail, to show the worker's backoff
/// retries and dead-lettering (and requeueing them, see README).
/// </summary>
public sealed partial class SendWelcomeEmail(ConsumerDbContext db, ILogger<SendWelcomeEmail> logger) : IInboxHandler<UserRegisteredIntegrationEvent>
{
    public const string BouncingDomain = "@bounce.test";

    public Task HandleAsync(UserRegisteredIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (integrationEvent.Email.EndsWith(BouncingDomain, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"The mail server rejected {integrationEvent.Email}.");

        db.Add(WelcomeEmail.Sent(integrationEvent.EventId, integrationEvent.UserId, integrationEvent.Email));
        LogSent(logger, integrationEvent.Email);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[ConsumerB] Welcome email sent to {Email}.")]
    private static partial void LogSent(ILogger logger, string email);
}
