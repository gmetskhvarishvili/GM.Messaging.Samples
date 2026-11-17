using GM.Messaging.Persistence.Protection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Messaging.Sample.Common.KeyManagement;

/// <summary>
/// Per-person keys for events with a <c>[DataSubject]</c>, in the shared <c>messaging_keys</c> database
/// (<c>ConnectionStrings:SubjectKeys</c>), registered next to <c>AddGMSensitiveData</c> in every host. GM.Messaging
/// creates the database and its <c>subject_keys</c> table at startup. The Data Protection for <c>dp:</c> keys comes
/// from <c>Messaging:SensitiveData:DataProtection</c>, so there's nothing else to set up here.
/// </summary>
public static class SampleKeyManagement
{
    public static IServiceCollection AddSampleKeyManagement(this IServiceCollection services, IConfiguration configuration) =>
        services.AddGMSubjectKeys(db => db
            .UseNpgsql(configuration.GetConnectionString("SubjectKeys"))
            .UseSnakeCaseNamingConvention());
}
