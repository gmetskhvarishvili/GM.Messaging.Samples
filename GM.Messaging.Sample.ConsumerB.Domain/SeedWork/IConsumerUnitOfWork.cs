using GM.EntityFramework.Domain.Repositories;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;

namespace GM.Messaging.Sample.ConsumerB.Domain.SeedWork;

public interface IConsumerUnitOfWork : IGenericUnitOfWork
{
    IInboxMessageRepository InboxMessageRepository { get; }
}
