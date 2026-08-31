using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Infrastructure;

public abstract class DesignTimeDbContextFactoryBase<TContext> :
    IDesignTimeDbContextFactory<TContext> where TContext : ConsumerDbContext    
{
    private const string ConnectionStringName = "ApplicationDatabase";
    private const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";

    public TContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory() + $"{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}GM.Messaging.Sample.ConsumerA.Worker";
        return Create(basePath, Environment.GetEnvironmentVariable(AspNetCoreEnvironment) ?? "Development");
    }

    protected abstract TContext CreateNewInstance(DbContextOptions<TContext> options);

    private TContext Create(string basePath, string environmentName)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.Local.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        return Create(connectionString);
    }

    private TContext Create(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException($"Connection string '{ConnectionStringName}' is null or empty.", nameof(connectionString));
        }

        Console.WriteLine($"DesignTimeDbContextFactoryBase.Create(string): Connection string: '{connectionString}'.");

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return CreateNewInstance(optionsBuilder.Options);
    }
}