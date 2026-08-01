using GM.Messaging.Persistence.Configuration;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;

namespace GM.Messaging.Sample.ConsumerB.Persistence.Configuration;

public class ConsumerInboxMessageConfiguration() : InboxMessageConfiguration<InboxMessage>("inbox_messages");
