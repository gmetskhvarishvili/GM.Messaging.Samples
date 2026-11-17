using GM.Messaging.Persistence.Protection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GM.Messaging.Sample.Common.KeyManagement;

/// <summary>
/// The <c>messaging_keys</c> database: one key per person (<c>subject_keys</c>), wrapped by the sensitive-data key
/// ring. It's shared: every service that writes or reads UserRegistered and ReturnRequested needs the same keys,
/// and deleting a person's key makes their personal data unreadable everywhere (crypto-shredding). The API
/// applies its migrations.
/// </summary>
public sealed class SubjectKeysDbContext(DbContextOptions<SubjectKeysDbContext> options) : DbContext(options)
{
    public const string Schema = "messaging";

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new SubjectKeyConfiguration(Schema));
}

/// <summary>For <c>dotnet ef migrations add ... --project GM.Messaging.Sample.Common</c>.</summary>
public sealed class SubjectKeysDbContextFactory : IDesignTimeDbContextFactory<SubjectKeysDbContext>
{
    public SubjectKeysDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SubjectKeysDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=messaging_keys;Username=postgres;Password=123456")
            .UseSnakeCaseNamingConvention()
            .Options);
}
