using GM.EntityFramework.Domain.Repositories;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;

namespace GM.Messaging.Sample.Domain.SeedWork;

public interface IUnitOfWork : IGenericUnitOfWork
{
    IOutboxMessageRepository OutboxMessageRepository { get; }
}
