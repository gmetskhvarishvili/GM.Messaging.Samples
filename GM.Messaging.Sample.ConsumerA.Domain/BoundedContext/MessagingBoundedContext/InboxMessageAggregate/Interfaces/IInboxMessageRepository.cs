using GM.EntityFramework.Domain.Repositories;

namespace GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;

public interface IInboxMessageRepository : IGenericRepository<InboxMessage>;
