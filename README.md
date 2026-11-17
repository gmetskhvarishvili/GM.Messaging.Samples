<p align="center">
  <img src="icon.png" alt="GM.Messaging Samples" width="140" height="140" />
</p>

# GM.Messaging Samples

[![CI](https://github.com/gmetskhvarishvili/GM.Messaging.Samples/actions/workflows/ci.yml/badge.svg)](https://github.com/gmetskhvarishvili/GM.Messaging.Samples/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A runnable, multi-service demo of **[GM.Messaging](https://www.nuget.org/packages/GM.Messaging)** — a
producer that publishes integration events through a transactional **outbox**, and two independent
consumers that receive them over **RabbitMQ** with an **inbox** for de-duplication. Built alongside
[GM.API](https://www.nuget.org/packages/GM.API), [GM.Mediator](https://www.nuget.org/packages/GM.Mediator),
[GM.EntityFramework](https://www.nuget.org/packages/GM.EntityFramework) and
[GM.Mapper](https://www.nuget.org/packages/GM.Mapper). Targets `net10.0`.

## Services

```
GM.Messaging.Samples/
├── Producer
│   ├── GM.Messaging.Sample.API             # Web API — commands write events to the outbox; admin; health
│   ├── GM.Messaging.Sample.Application      # CQRS commands + validators (Create Order/Payment/…)
│   ├── GM.Messaging.Sample.Domain           # Integration events + the outbox aggregate
│   ├── GM.Messaging.Sample.Persistence      # EF Core persistence (PostgreSQL)
│   ├── GM.Messaging.Sample.Infrastructure   # GM.Messaging (Wolverine + RabbitMQ) for the producer worker
│   ├── GM.Messaging.Sample.Common           # Shared resources (exceptions)
│   └── GM.Messaging.Sample.Producer.Worker  # The outbox relay (leased, confirmed, retried, ordered); health
├── ConsumerA (Domain / Infrastructure / Persistence / Worker)   # customer activity via ProcessAsync
├── ConsumerB (Domain / Infrastructure / Persistence / Worker)   # order timeline, stock, inbox-worker emails
├── tests/
│   ├── GM.Messaging.Sample.Tests            # validator, event and outbox unit tests
│   └── GM.Messaging.Sample.StackTests       # GM.Testing.Messaging.Integration checks against the running stack
├── docs/                                    # architecture and operations
├── deploy/                                  # RabbitMQ (TLS, users), Data Protection keys
└── Dockerfile, docker-compose.yml           # the whole stack
```

Only the published **GM.*** packages are referenced — no project references into the library repos.

## Run it

```bash
docker compose up -d --build
```

| What | Where |
| --- | --- |
| API + Swagger | http://localhost:8080 |
| RabbitMQ management | http://localhost:15672 (gm-admin / gm-admin-secret); AMQP is TLS-only on 5671 |
| Consumer A / B, producer worker | http://localhost:8081, :8082, :8083: `/health/ready` |
| Admin endpoints (every host) | `/admin/messaging/...` (open in the sample) |

[`GM.Messaging.Sample.API.http`](GM.Messaging.Sample.API/GM.Messaging.Sample.API.http) has a request for
every endpoint, including the failure cases and the workers' admin endpoints. To run the projects with
`dotnet run` instead, start only the infrastructure (`docker compose up -d postgres
rabbitmq-setup`) and start the API before the producer worker: the API applies the application
database's migrations and the worker waits for them. Locally the workers listen on ports 5081 to 5083.

## What it demonstrates

Every GM.Messaging feature, each where a real service would use it:

| Feature | Where |
| --- | --- |
| **Transactional outbox**: an event is saved with the change that raised it | API commands (`OutboxMessage.Create`) |
| **Leased relay** with publisher confirms, backoff retries and dead-lettering; safe to scale | Producer worker (`AddGMOutbox(..., relay: true)`); CI runs two with `--scale producer-worker=2` |
| **Instant publish across processes**: a save in the API wakes the relays over RabbitMQ | API (`AddGMOutbox(..., relay: false)`); the relays poll only every 30 s, and CI checks events go out within seconds |
| **Stable event names**, also used for routing | `[IntegrationEventName("orders.order-placed")]`; `ProducerTopology` routes by name |
| **Ordering keys** and a **sequential** consumer queue | Order and payment events keyed by order; ConsumerB's `order-timeline` queue |
| **Event versions** with an upcaster, for stored rows and for events on the wire | `InventoryUpdated` v2 (`Warehouse` → `WarehouseCode`), registered with `AddGMIntegrationEvents` |
| **Transactional inbox**: the handler's changes and the inbox row commit together | ConsumerA (`customer_activities`), ConsumerB (`order_timeline`, `stock_levels`) via `ProcessAsync` |
| **Retried failures** recorded on the inbox row, then the broker's dead-letter queue | ConsumerA rejects orders in `XXX`; CI fixes the cause, replays the dead letter and checks it was handled |
| **Inbox worker** for slow work, with backoff and dead-lettering | ConsumerB's welcome emails (`IngestAsync` + `IInboxHandler`); `@bounce.test` addresses fail |
| **Admin endpoints** for the outbox, inbox, broker queues and subject keys, behind your authorization policy | `MapGMMessagingAdmin()` (GM.Messaging.AspNetCore) in every host under `/admin/messaging`; open in the sample (`AllowAnonymous`), behind your own policy in a real service. Message bodies show with `[SensitiveData]` masked (`BrokerMessageBodies.Redacted`) |
| **No lost unrouted messages**: anything published with no queue bound is kept | The API's `ReturnRequested` has no consumer yet, so it waits in `gm.unrouted`; `POST /admin/messaging/broker/unrouted/move?exchange=gm.events` sends it on once a queue exists |
| **Health checks** in every host: database, RabbitMQ, backlog, broker queues | `/health/ready` (`AddGMDatabaseCheck`, `AddGMMessagingCheck`, `AddGMMessagingBacklogCheck`; the workers add `AddGMBrokerQueueCheck`) |
| **Broker queue depths**: what waits on RabbitMQ | GM.Messaging reads each worker's queues every 30 s for the broker queue health check |
| **One consumer name per service** | `Messaging:Inbox:ConsumerName`; handlers call `ProcessAsync(message, handler)` |
| **Retention** | Published outbox rows go after 7 days, handled inbox rows after 30 |
| **Personal data in events**: only where needed, and encrypted | `UserRegistered.Email`/`Name` and `ReturnRequested.ContactEmail` are `[SensitiveData]` (set up by `AddGMOutbox`/`AddGMInbox` from the key ring in `Messaging:SensitiveData`): ciphertext in the outbox, the inbox and on RabbitMQ; other events carry ids only |
| **A key per person, and erasure** (crypto-shredding) | `[DataSubject]` on `UserId`/`CustomerId`; keys in the shared `messaging_keys` database (`ConnectionStrings:SubjectKeys`, set up by `AddGMOutbox`/`AddGMInbox`). `DELETE /admin/messaging/subject-keys/{id}` makes that person's data unreadable everywhere, and `gm.subject-forgotten` (`AddGMOutbox` registers it) has both consumers erase their own copies |
| **Key rotation**, with the new key kept out of configuration | `sample-2026-12` is a `dp:` value (Data Protection, keys in `deploy/dataprotection`); CI makes it current and re-wraps every person's key (`POST /admin/messaging/subject-keys/rewrap`) |
| **Quorum queues**: every durable queue replicated across the cluster | `Messaging:RabbitMq:QueueType: Quorum` in every service; CI checks all 10 `gm.*` queues are quorum |
| **Queue limits**: a full queue refuses messages, which wait in the outbox; old dead letters expire | `MaxQueueLength: 100000` (`reject-publish`) on the consumer queues, `DeadLetterRetentionHours` / `UnroutedRetentionHours: 336` |
| **Consumer throughput** | `ConsumerTopology:PrefetchCount: 50` in ConsumerB; its inventory queue overrides it with `PrefetchCount: 20, MaxParallelMessages: 4` |
| **Scheduled events**: saved now, published when due | `POST /api/v1/reminders` saves a `ReminderDue` with `PublishAt`; ConsumerA records it once it's due (CI checks it doesn't arrive early) |
| **Large events** through a claim check | `POST /api/v1/inventory/import` (thousands of items, ~200 KB) goes over `Messaging:LargeMessages:OffloadAboveBytes: 65536`: the body in the shared `gm-claim-checks` volume, a reference on RabbitMQ; ConsumerB applies it |
| **A secured broker**: TLS only, a user per service, mutual TLS | A sample CA (`deploy/tls/generate.sh`) and `Messaging:RabbitMq:TlsCaCertificatePath`; per-service users with scoped permissions (`deploy/rabbitmq/setup.sh`); ConsumerA signs in with a client certificate (`TlsClientCertificatePath`, `UseExternalAuthentication`) |

[docs/architecture.md](docs/architecture.md) walks one event through the system and lists what happens
on each kind of failure; [docs/operations.md](docs/operations.md) covers dead letters, keys and erasure,
health and settings.

## How GM.Messaging is wired

```csharp
// Producer persistence: one GM.Messaging call. AddGMOutbox registers the outbox (admin, erasure events, and the relay in
// the producer worker or, in the API, wake-ups that make every relay publish at once), attaches GM's interceptors to
// ApplicationDbContext, and sets up personal data from configuration: the key ring and the per-person key database,
// on the same Npgsql and snake_case settings as ApplicationDbContext. Consumers: AddGMInbox(configuration), likewise.
services.AddDbContextPool<ApplicationDbContext>(o => o.UseNpgsql(cs).UseSnakeCaseNamingConvention());
services.AddGMOutbox<OutboxMessage, ApplicationDbContext>(configuration, relay: runOutboxRelay);

// Event versions, wherever events are read (the relay, and consumers for older producers on the wire).
builder.Services.AddGMIntegrationEvents(events => events
    .AddAssembly(typeof(SampleEventVersions).Assembly)
    .AddUpcaster<InventoryUpdatedIntegrationEvent>(fromVersion: 1, SampleEventVersions.InventoryUpdatedV1ToV2));

// Consumers: the inbox. Handlers write through IConsumerUnitOfWork: AddGMInbox has Wolverine resolve ConsumerDbContext
// from the message's scope, so the repositories use the same instance IInboxProcessor commits.
builder.Services.AddInfrastructure(builder.Configuration);   // AddGMMessaging
services.AddGMInbox<InboxMessage, ConsumerDbContext>();

// ConsumerB: the inbox worker and its handler.
builder.Services.AddGMInboxWorker<InboxMessage, ConsumerDbContext>();
builder.Services.AddGMInboxHandler<UserRegisteredIntegrationEvent, SendWelcomeEmail>();

// Every host: GM.Messaging's admin endpoints, behind your authorization policy.
app.MapGMMessagingAdmin(admin => admin.AuthorizationPolicy = "your-policy");
```

A consumer handler:

```csharp
public static Task Handle(OrderPlacedIntegrationEvent message, IInboxProcessor inbox, IConsumerUnitOfWork unitOfWork, CancellationToken ct) =>
    inbox.ProcessAsync(message, () =>
        // commits with the inbox row, or not at all; ProcessAsync saves, so no SaveChanges here
        unitOfWork.CustomerActivityRepository.AddAsync(CustomerActivity.Record(...), ct), ct);
```

The `outbox_messages` and `inbox_messages` tables live in a shared `messaging` schema. The API migrates
the application database (the producer worker waits for it); each consumer migrates its own.

## Testing

```bash
dotnet test
```

Unit tests cover the command validators, the events (stable names, ordering keys, the version-2 upcaster,
trace capture, whose key encrypts personal data) and outbox serialization. CI's `compose` job also runs
the whole stack from `docker-compose.yml` with two relays and drives it through the API. It checks that:

- each consumer handled every event once, and ConsumerB's timeline has an order before its payment;
- the failing order was retried and reached the broker's dead-letter queue, and the bouncing email was dead-lettered in the inbox;
- after the cause is fixed, replaying the dead-lettered order gets it handled, and an unrouted message is
  moved to the queue created for it;
- a version-1 `InventoryUpdated` published straight to RabbitMQ is upcast and handled;
- each user's data is encrypted with their own key, and an erasure request deletes the key and both consumers erase their own copies;
- ConsumerA is connected with its client certificate (EXTERNAL) and has no password;
- after switching to the `dp:`-protected key, re-wrapping moves every person's key to it, and events still flow;
- a save in the API is published within seconds, although the relays poll every 30 s;
- both relays are connected, and RabbitMQ received every event exactly once;
- the admin endpoints map what each host has, and with every host's
  health checks report the backlog; nothing went unrouted.

Then `tests/GM.Messaging.Sample.StackTests` runs against the same stack (`SAMPLE_STACK=true`) with the
GM.Testing.Messaging.Integration assertions. It checks that the outbox is drained, and that Ana's
registration is stored encrypted and was handled by both consumers. It also checks that the bounce is
dead-lettered in ConsumerB's inbox, the recovered order was handled, and nothing is left unrouted.
Finally, it checks that the return request's contact email decrypts with the customer's key, the erased
user's email and name read as `null`, and every person's key is wrapped with the new key.
Without `SAMPLE_STACK` these tests skip.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker, for `docker-compose.yml` (PostgreSQL, RabbitMQ and the four services).

## License

MIT — see [LICENSE](LICENSE).
