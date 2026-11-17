using GM.EntityFramework.Persistence.Repositories;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext.Interfaces;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;

namespace GM.Messaging.Sample.ConsumerB.Persistence.Repositories;

public class OrderTimelineEntryRepository(ConsumerDbContext context)
    : GenericRepository<OrderTimelineEntry, ConsumerDbContext>(context), IOrderTimelineEntryRepository;
