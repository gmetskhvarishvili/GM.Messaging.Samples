using GM.EntityFramework.Persistence.Repositories;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Repositories;

public class InboxMessageRepository(ConsumerDbContext context)
    : GenericRepository<InboxMessage, ConsumerDbContext>(context), IInboxMessageRepository;
