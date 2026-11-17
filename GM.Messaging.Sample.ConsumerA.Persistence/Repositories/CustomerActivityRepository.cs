using GM.EntityFramework.Persistence.Repositories;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext.Interfaces;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Repositories;

public class CustomerActivityRepository(ConsumerDbContext context)
    : GenericRepository<CustomerActivity, ConsumerDbContext>(context), ICustomerActivityRepository;
