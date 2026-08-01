using GM.Messaging.Persistence.Configuration;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;

namespace GM.Messaging.Sample.Persistence.Configuration;

public class SampleOutboxMessageConfiguration() : OutboxMessageConfiguration<OutboxMessage>("outbox_messages");
