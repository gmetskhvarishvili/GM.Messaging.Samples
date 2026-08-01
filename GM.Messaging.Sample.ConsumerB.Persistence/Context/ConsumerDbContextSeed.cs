using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.ConsumerB.Persistence.Context;

public class ConsumerDbContextSeed
{
    public Task SeedAsync(ConsumerDbContext context, ILogger<ConsumerDbContextSeed> logger)
    {
        return Task.CompletedTask;
    }
}
