using GM.Messaging.Sample.ConsumerB.Persistence.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GM.Messaging.Sample.ConsumerB.Persistence.Context;

public class ConsumerDbContextFactory : DesignTimeDbContextFactoryBase<ConsumerDbContext>
{
    protected override ConsumerDbContext CreateNewInstance(DbContextOptions<ConsumerDbContext> options)
        => new(options);
}
