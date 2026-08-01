using GM.Messaging.Persistence.Configuration;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Configuration;

public class ConsumerInboxMessageConfiguration() : InboxMessageConfiguration<InboxMessage>("inbox_messages");
