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
│   ├── GM.Messaging.Sample.API             # Web API — commands write events to the outbox; outbox admin; health
│   ├── GM.Messaging.Sample.Application      # CQRS commands + validators (Create Order/Payment/…)
│   ├── GM.Messaging.Sample.Domain           # Integration events + the outbox aggregate
│   ├── GM.Messaging.Sample.Persistence      # EF Core persistence (PostgreSQL)
│   ├── GM.Messaging.Sample.Common           # Shared resources + OpenTelemetry setup
│   └── GM.Messaging.Sample.Producer.Worker  # The outbox relay (leased, confirmed, retried, ordered); health
├── ConsumerA (Domain / Infrastructure / Persistence / Worker)   # customer activity via ProcessAsync
├── ConsumerB (Domain / Infrastructure / Persistence / Worker)   # order timeline, stock, inbox-worker emails, admin
├── tests/
│   └── GM.Messaging.Sample.Tests            # validator, event and outbox unit tests
├── docs/                                    # architecture and operations
├── deploy/                                  # OpenTelemetry collector and Prometheus config
└── Dockerfile, docker-compose.yml           # the whole stack, with traces and metrics
```

Only the published **GM.*** packages are referenced — no project references into the library repos.

## Run it

```bash
docker compose up -d --build
```

| What | Where |
| --- | --- |
| API + Swagger | http://localhost:8080 |
| RabbitMQ management | http://localhost:15672 (guest/guest) |
| Jaeger (traces) | http://localhost:16686 |
| Prometheus (metrics) | http://localhost:9090, e.g. `gm_messaging_outbox_published_total` |
| Consumer A / B, producer worker | http://localhost:8081, :8082, :8083: `/health/ready`; ConsumerB also `/admin/...` |

[`GM.Messaging.Sample.API.http`](GM.Messaging.Sample.API/GM.Messaging.Sample.API.http) has a request for
every endpoint, including the failure cases and the workers' admin endpoints. To run the projects with
`dotnet run` instead, start only the infrastructure (`docker compose up -d postgres rabbitmq otel-collector
jaeger prometheus`) and start the API before the producer worker: the API applies the application
database's migrations and the worker waits for them. Locally the workers listen on ports 5081 to 5083.

## What it demonstrates

Every GM.Messaging feature, each where a real service would use it:

| Feature | Where |
| --- | --- |
| **Transactional outbox**: an event is saved with the change that raised it | API commands (`OutboxMessage.From`) |
| **Leased relay** with publisher confirms, backoff retries and dead-lettering; safe to scale | Producer worker (`AddGMOutboxRelay`); CI runs two with `--scale producer-worker=2` |
| **Instant publish across processes**: a save in the API wakes the relays over RabbitMQ | API (`AddGMOutboxWakeUp`); the relays poll only every 30 s, and CI checks events go out within seconds |
| **Stable event names**, also used for routing | `[IntegrationEventName("orders.order-placed")]`; `ProducerTopology` routes by name |
| **Ordering keys** and a **sequential** consumer queue | Order and payment events keyed by order; ConsumerB's `order-timeline` queue |
| **Event versions** with an upcaster, for stored rows and for events on the wire | `InventoryUpdated` v2 (`Warehouse` → `WarehouseCode`), registered with `AddGMIntegrationEvents` |
| **Transactional inbox**: the handler's changes and the inbox row commit together | ConsumerA (`customer_activities`), ConsumerB (`order_timeline`, `stock_levels`) via `ProcessAsync` |
| **Retried failures** recorded on the inbox row, then the broker's dead-letter queue | ConsumerA rejects orders in `XXX` |
| **Inbox worker** for slow work, with backoff and dead-lettering | ConsumerB's welcome emails (`IngestAsync` + `IInboxHandler`); `@bounce.test` addresses fail |
| **Dead-letter admin**: outbox, inbox, and the broker's queues | API `/api/v1/admin/outbox/...`; ConsumerB `/admin/inbox/...`, `/admin/broker/dead-letters/...`, `/admin/broker/unrouted/...` |
| **No lost unrouted messages**: anything published with no queue bound is kept | The `gm.unrouted` queue; `POST /admin/broker/unrouted/move?exchange=gm.events` sends them back |
| **Health checks** in every host: database, RabbitMQ, backlog | `/health/ready` (`AddGMDatabaseCheck`, `AddGMMessagingCheck`, `AddGMMessagingBacklogCheck`) |
| **Traces and metrics** | OpenTelemetry in every host (`AddSampleTelemetry`) → OpenTelemetry collector → Jaeger and Prometheus |
| **One consumer name per service** | `Messaging:Inbox:ConsumerName`; handlers call `ProcessAsync(message, handler)` |
| **Retention** | Published outbox rows go after 7 days, handled inbox rows after 30 |

[docs/architecture.md](docs/architecture.md) walks one event through the system and lists what happens
on each kind of failure; [docs/operations.md](docs/operations.md) covers dead letters, health, traces,
metrics and settings.

## How GM.Messaging is wired

```csharp
// Producer persistence: outbox admin everywhere; the relay in the producer worker; in the API, wake-ups
// that make every relay publish as soon as a save adds outbox messages.
services.AddDbContextPool<ApplicationDbContext>((sp, o) => o.UseNpgsql(cs).AddGMOutboxSignal(sp));
services.AddGMOutboxAdministration<OutboxMessage, ApplicationDbContext>();
if (runOutboxRelay)
    services.AddGMOutboxRelay<OutboxMessage, ApplicationDbContext>();
else
    services.AddGMOutboxWakeUp(configuration);

// Event versions, wherever events are read (the relay, and consumers for older producers on the wire).
builder.Services.AddGMIntegrationEvents(events => events
    .AddAssembly(typeof(SampleEventVersions).Assembly)
    .AddUpcaster<InventoryUpdatedIntegrationEvent>(fromVersion: 1, SampleEventVersions.InventoryUpdatedV1ToV2));

// Consumers: the inbox. Handlers can take ConsumerDbContext too: AddGMInbox has Wolverine resolve it
// from the message's scope, so it's the same instance IInboxProcessor commits.
builder.Services.AddInfrastructure(builder.Configuration);   // AddGMMessaging + OpenTelemetry
services.AddGMInbox<InboxMessage, ConsumerDbContext>();

// ConsumerB: the inbox worker and its handler.
builder.Services.AddGMInboxWorker<InboxMessage, ConsumerDbContext>();
builder.Services.AddGMInboxHandler<UserRegisteredIntegrationEvent, SendWelcomeEmail>();
```

A consumer handler:

```csharp
public static Task Handle(OrderPlacedIntegrationEvent message, IInboxProcessor inbox, ConsumerDbContext db, CancellationToken ct) =>
    inbox.ProcessAsync(message, () =>
    {
        db.Add(CustomerActivity.Record(...)); // commits with the inbox row, or not at all
        return Task.CompletedTask;
    }, ct);
```

The `outbox_messages` and `inbox_messages` tables live in a shared `messaging` schema. The API migrates
the application database (the producer worker waits for it); each consumer migrates its own.

## Testing

```bash
dotnet test
```

Unit tests cover the command validators, the events (stable names, ordering keys, the version-2 upcaster,
trace capture) and outbox serialization. CI's `compose` job also runs the whole stack from
`docker-compose.yml` with two relays and drives it through the API. It checks that:

- each consumer handled every event once, and ConsumerB's timeline has an order before its payment;
- the failing order was retried and reached the broker's dead-letter queue, and the bouncing email was dead-lettered in the inbox;
- a save in the API is published within seconds, although the relays poll every 30 s;
- Prometheus shows both relays, every event published exactly once, and no inbox duplicates;
- the admin endpoints and every host's health checks report the backlog, and nothing went unrouted.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker, for `docker-compose.yml` (PostgreSQL, RabbitMQ, the OpenTelemetry collector, Jaeger, Prometheus
  and the four services).

## License

MIT — see [LICENSE](LICENSE).
