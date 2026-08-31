using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.Persistence.Context;

public class ApplicationDbContextSeed
{
    // Never instantiated: this type exists only as the ILogger<T> category marker for SeedAsync below.
    protected ApplicationDbContextSeed()
    {
    }

    public static Task SeedAsync(ApplicationDbContext context, ILogger<ApplicationDbContextSeed> logger)
    {
        return Task.CompletedTask;
    }
}
