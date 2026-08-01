using GM.Messaging.Sample.ConsumerA.Persistence.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Context;

public class ConsumerDbContextFactory : DesignTimeDbContextFactoryBase<ConsumerDbContext>
{
    protected override ConsumerDbContext CreateNewInstance(DbContextOptions<ConsumerDbContext> options)
        => new(options);
}
