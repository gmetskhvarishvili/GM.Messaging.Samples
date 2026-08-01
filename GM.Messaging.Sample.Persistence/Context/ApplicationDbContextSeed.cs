using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.Persistence.Context;

public class ApplicationDbContextSeed
{
    public Task SeedAsync(ApplicationDbContext context, ILogger<ApplicationDbContextSeed> logger)
    {
        return Task.CompletedTask;
    }
}
