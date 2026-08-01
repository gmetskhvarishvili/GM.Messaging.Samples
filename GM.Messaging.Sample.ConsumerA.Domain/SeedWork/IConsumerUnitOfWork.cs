using GM.EntityFramework.Domain.Repositories;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;

namespace GM.Messaging.Sample.ConsumerA.Domain.SeedWork;

public interface IConsumerUnitOfWork : IGenericUnitOfWork
{
    IInboxMessageRepository InboxMessageRepository { get; }
}
