using GM.Messaging.Persistence.Protection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Messaging.Sample.Common.KeyManagement;

/// <summary>
/// The key management every sample host shares, next to <c>AddGMSensitiveData</c>:
/// <list type="bullet">
/// <item>ASP.NET Core Data Protection with the sample's key ring (<c>DataProtection:KeysPath</c>), which unprotects
/// <c>dp:</c> values in <c>Messaging:SensitiveData:Keys</c>, so configuration holds no raw key. In production,
/// keep Data Protection's keys in a shared store protected by Key Vault or KMS.</item>
/// <item>Per-person keys in the shared <c>messaging_keys</c> database (<c>ConnectionStrings:SubjectKeys</c>),
/// for events with a <c>[DataSubject]</c>.</item>
/// </list>
/// </summary>
public static class SampleKeyManagement
{
    public const string ApplicationName = "GM.Messaging.Sample";

    public static IServiceCollection AddSampleKeyManagement(this IServiceCollection services, IConfiguration configuration)
    {
        var keysPath = configuration["DataProtection:KeysPath"]
            ?? throw new InvalidOperationException("DataProtection:KeysPath isn't configured.");

        // Every host reads the same Data Protection keys; none creates new ones (the directory is read-only in docker).
        services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .DisableAutomaticKeyGeneration();

        services.AddDbContext<SubjectKeysDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("SubjectKeys"))
            .UseSnakeCaseNamingConvention());
        services.AddGMSubjectKeys<SubjectKeysDbContext>();

        return services;
    }
}
