using GM.EntityFramework.Persistence.Repositories;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext.Interfaces;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;

namespace GM.Messaging.Sample.ConsumerB.Persistence.Repositories;

public class WelcomeEmailRepository(ConsumerDbContext context)
    : GenericRepository<WelcomeEmail, ConsumerDbContext>(context), IWelcomeEmailRepository;
