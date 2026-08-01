using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Context;

public class ConsumerDbContextSeed
{
    public Task SeedAsync(ConsumerDbContext context, ILogger<ConsumerDbContextSeed> logger)
    {
        return Task.CompletedTask;
    }
}
