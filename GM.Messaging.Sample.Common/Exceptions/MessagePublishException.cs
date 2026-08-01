namespace GM.Messaging.Sample.Common.Exceptions;

public class MessagePublishException(string message, Exception? innerException = null)
    : Exception(message, innerException);
