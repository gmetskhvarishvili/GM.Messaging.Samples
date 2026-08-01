using GM.EntityFramework.Domain.Repositories;

namespace GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;

public interface IInboxMessageRepository : IGenericRepository<InboxMessage>;
