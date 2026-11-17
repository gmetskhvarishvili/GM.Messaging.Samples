using GM.Messaging.Broker;
using GM.Messaging.Domain.Events;
using GM.Messaging.Options;
using GM.Messaging.Persistence.Protection;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events;
using GM.Messaging.Sample.Domain.Events.Orders;
using GM.Messaging.Sample.Domain.Events.Returns;
using GM.Messaging.Sample.Domain.Events.Users;
using GM.Messaging.Sample.Persistence.Context;
using GM.Testing.Messaging.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ConsumerAContext = GM.Messaging.Sample.ConsumerA.Persistence.Context.ConsumerDbContext;
using ConsumerAInbox = GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.InboxMessage;
using ConsumerBContext = GM.Messaging.Sample.ConsumerB.Persistence.Context.ConsumerDbContext;
using ConsumerBInbox = GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.InboxMessage;

namespace GM.Messaging.Sample.StackTests;

/// <summary>
/// The sample's guarantees, checked against the running docker-compose stack with GM.Testing.Messaging.Integration
/// (after CI has placed orders, registered users, recovered the XXX order and moved the unrouted message).
/// </summary>
public class StackTests
{
    private const string SkipReason = "Set SAMPLE_STACK=true with the docker-compose stack running (CI's compose job does).";
    private static bool StackRunning => string.Equals(Environment.GetEnvironmentVariable("SAMPLE_STACK"), "true", StringComparison.OrdinalIgnoreCase);

    // Reads events the way the API does, from its appsettings.json: its key ring (sample-2026-12 through Data
    // Protection, keys in deploy/dataprotection) and the shared per-person key database.
    private static readonly Lazy<MessagingReader> Reader = new(() => MessagingReader.FromAppSettings(
        Path.Combine(RepositoryRoot(), "GM.Messaging.Sample.API", "appsettings.json"),
        configureServices: (services, configuration) => services.AddGMSubjectKeys(db => db
            .UseNpgsql(configuration.GetConnectionString("SubjectKeys")).UseSnakeCaseNamingConvention()),
        eventAssemblies: typeof(SampleEventVersions).Assembly));

    private static IntegrationEventRegistry Registry => Reader.Value.Registry;

    private static SubjectKeysDbContext Keys() => Reader.Value.Services.CreateScope().ServiceProvider.GetRequiredService<SubjectKeysDbContext>();

    private static TContext Context<TContext>(string database, Func<DbContextOptions<TContext>, TContext> create)
        where TContext : DbContext =>
        create(new DbContextOptionsBuilder<TContext>()
            .UseNpgsql($"Host=localhost;Port=5432;Database={database};Username=postgres;Password=123456")
            .UseSnakeCaseNamingConvention()
            .Options);

    private static ApplicationDbContext Producer() => Context<ApplicationDbContext>("messaging_api", o => new ApplicationDbContext(o));

    private static ConsumerAContext ConsumerA() => Context<ConsumerAContext>("messaging_consumer_a", o => new ConsumerAContext(o));

    private static ConsumerBContext ConsumerB() => Context<ConsumerBContext>("messaging_consumer_b", o => new ConsumerBContext(o));

    private static IBrokerQueueAdministration Broker() => BrokerQueues.Connect(new MessagingOptions
    {
        RabbitMq = new RabbitMqOptions
        {
            HostName = "localhost",
            Port = 5671,
            UseTls = true,
            TlsCaCertificatePath = Path.Combine(RepositoryRoot(), "deploy", "tls", "certs", "ca.pem"),
            UserName = "gm-admin",
            Password = "gm-admin-secret",
        },
    });

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("docker-compose.yml not found above the test output.");
    }

    [SkippableFact]
    public async Task Outbox_IsDrained()
    {
        Skip.IfNot(StackRunning, SkipReason);
        await using var producer = Producer();
        await Eventually.AssertAsync(() => producer.ShouldHaveNoPendingOutboxAsync<OutboxMessage>());
    }

    [SkippableFact]
    public async Task Registration_TravelsEncrypted_AndBothConsumersHandledIt()
    {
        Skip.IfNot(StackRunning, SkipReason);
        await using var producer = Producer();
        var registered = await producer.ShouldHaveOutboxEventAsync<OutboxMessage, UserRegisteredIntegrationEvent>(
            e => e.Email == "ana@example.test", Registry);

        Assert.False(await producer.Set<OutboxMessage>().AnyAsync(m => m.Payload.Contains("ana@example.test")));
        await using var consumerA = ConsumerA();
        await using var consumerB = ConsumerB();
        await consumerA.ShouldHaveHandledAsync<ConsumerAInbox>(registered.EventId, "consumer-a");
        await consumerB.ShouldHaveHandledAsync<ConsumerBInbox>(registered.EventId, "consumer-b");
    }

    [SkippableFact]
    public async Task BouncedWelcomeEmail_IsDeadLetteredInConsumerBsInbox()
    {
        Skip.IfNot(StackRunning, SkipReason);
        await using var producer = Producer();
        var bounce = await producer.ShouldHaveOutboxEventAsync<OutboxMessage, UserRegisteredIntegrationEvent>(
            e => e.Email == "nobody@bounce.test", Registry);

        await using var consumerB = ConsumerB();
        await consumerB.ShouldHaveDeadLetteredAsync<ConsumerBInbox>(bounce.EventId, "consumer-b");
    }

    [SkippableFact]
    public async Task RecoveredOrder_WasHandledByConsumerA_AndNothingIsLeftOnTheBroker()
    {
        Skip.IfNot(StackRunning, SkipReason);
        await using var producer = Producer();
        var order = await producer.ShouldHaveOutboxEventAsync<OutboxMessage, OrderPlacedIntegrationEvent>(e => e.Currency == "XXX", Registry);

        await using var consumerA = ConsumerA();
        await consumerA.ShouldHaveHandledAsync<ConsumerAInbox>(order.EventId, "consumer-a");

        var broker = Broker();
        try
        {
            await broker.ShouldHaveNoUnroutedAsync();
            await broker.ShouldHaveQueueCountAsync("gm.returns", 1); // the ReturnRequested moved from gm.unrouted
        }
        finally
        {
            await ((IAsyncDisposable)broker).DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task ReturnRequest_CarriesTheContactEmailEncryptedWithTheCustomersKey()
    {
        Skip.IfNot(StackRunning, SkipReason);
        await using var producer = Producer();
        var request = await producer.ShouldHaveOutboxEventAsync<OutboxMessage, ReturnRequestedIntegrationEvent>(e => e.Reason == "Wrong size", Registry);

        Assert.Equal("ana@example.test", request.ContactEmail);
        await using var keys = Keys();
        Assert.True(await keys.Set<SubjectKey>().AnyAsync(k => k.SubjectId == request.CustomerId.ToString()));
    }

    [SkippableFact]
    public async Task ErasedUser_TheirEventsEmailAndNameAreUnreadable()
    {
        Skip.IfNot(StackRunning, SkipReason);
        var erased = Guid.Parse(Environment.GetEnvironmentVariable("ERASED_USER")
            ?? throw new InvalidOperationException("ERASED_USER isn't set (CI's erasure step sets it)."));

        await using var producer = Producer();
        var registered = await producer.ShouldHaveOutboxEventAsync<OutboxMessage, UserRegisteredIntegrationEvent>(e => e.UserId == erased, Registry);

        // The event is still there, ids intact; the personal data in it died with the user's key.
        Assert.Null(registered.Email);
        Assert.Null(registered.Name);
        await using var keys = Keys();
        Assert.False(await keys.Set<SubjectKey>().AnyAsync(k => k.SubjectId == erased.ToString()));
    }

    [SkippableFact]
    public async Task AfterTheRotation_EveryUsersKeyIsWrappedWithTheNewKey()
    {
        Skip.IfNot(StackRunning, SkipReason);
        await using var keys = Keys();

        Assert.NotEmpty(await keys.Set<SubjectKey>().ToListAsync());
        Assert.True(await keys.Set<SubjectKey>().AllAsync(k => k.WrappedKey.StartsWith("k1:sample-2026-12:")));
    }
}
