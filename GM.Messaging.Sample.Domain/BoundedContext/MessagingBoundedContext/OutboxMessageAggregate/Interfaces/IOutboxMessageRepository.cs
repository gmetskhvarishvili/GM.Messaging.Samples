using GM.EntityFramework.Domain.Repositories;

namespace GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;

public interface IOutboxMessageRepository : IGenericRepository<OutboxMessage>;
