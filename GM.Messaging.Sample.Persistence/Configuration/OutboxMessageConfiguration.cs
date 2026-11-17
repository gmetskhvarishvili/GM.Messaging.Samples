using GM.Messaging.Persistence.Configuration;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;

namespace GM.Messaging.Sample.Persistence.Configuration;

public class OutboxMessageConfiguration() : GMOutboxMessageConfiguration<OutboxMessage>("messaging");
