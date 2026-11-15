using GM.EntityFramework.Persistence.Repositories;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;

namespace GM.Messaging.Sample.ConsumerB.Persistence.Repositories;

public class InboxMessageRepository(ConsumerDbContext context)
    : GenericRepository<InboxMessage, ConsumerDbContext>(context), IInboxMessageRepository;
