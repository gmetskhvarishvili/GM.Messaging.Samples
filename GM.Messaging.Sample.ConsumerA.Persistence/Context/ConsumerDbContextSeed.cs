using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Context;

public class ConsumerDbContextSeed
{
    // Never instantiated: this type exists only as the ILogger<T> category marker for SeedAsync below.
    protected ConsumerDbContextSeed()
    {
    }

    public static Task SeedAsync(ConsumerDbContext context, ILogger<ConsumerDbContextSeed> logger)
    {
        return Task.CompletedTask;
    }
}
