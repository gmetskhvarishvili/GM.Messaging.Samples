namespace GM.Messaging.Sample.Common.Exceptions;

public class MessageDeadLetteredException(Guid messageId, string reason)
    : Exception($"Message {messageId} was dead-lettered: {reason}")
{
    public Guid MessageId { get; } = messageId;
    public string Reason { get; } = reason;
}
