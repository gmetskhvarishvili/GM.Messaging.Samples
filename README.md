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
│   └── GM.Messaging.Sample.Producer.Worker  # The outbox relay (leased, confirmed, retried, ordered)
├── ConsumerA (Domain / Infrastructure / Persistence / Worker)   # customer activity via ProcessAsync
├── ConsumerB (Domain / Infrastructure / Persistence / Worker)   # order timeline, stock, inbox-worker emails
├── tests/
│   └── GM.Messaging.Sample.Tests            # validator, event and outbox unit tests
├── docs/                                    # architecture and operations
└── Dockerfile, docker-compose.yml           # the whole stack, with Jaeger
```

Only the published **GM.*** packages are referenced — no project references into the library repos.

## Run it

```bash
docker compose up -d --build
```

The API (Swagger) is on http://localhost:8080, RabbitMQ's UI on http://localhost:15672 (guest/guest) and
Jaeger on http://localhost:16686. [`GM.Messaging.Sample.API.http`](GM.Messaging.Sample.API/GM.Messaging.Sample.API.http)
has a request for every endpoint, including the failure cases. To run the projects with `dotnet run`
instead, start only the infrastructure: `docker compose up -d postgres rabbitmq jaeger`.

## What it demonstrates

Every GM.Messaging feature, each where a real service would use it:

| Feature | Where |
| --- | --- |
| **Transactional outbox**: an event is saved with the change that raised it | API commands (`OutboxMessage.From`) |
| **Leased relay** with publisher confirms, backoff retries and dead-lettering; safe to scale | Producer worker (`AddGMOutboxRelay`); `docker compose up --scale producer-worker=2` |
| **Stable event names**, also used for routing | `[IntegrationEventName("orders.order-placed")]`; `ProducerTopology` routes by name |
| **Ordering keys** and a **sequential** consumer queue | Order and payment events keyed by order; ConsumerB's `order-timeline` queue |
| **Event versions** with an upcaster | `InventoryUpdated` v2 (`Warehouse` → `WarehouseCode`), `SampleEventVersions` |
| **Transactional inbox**: the handler's changes and the inbox row commit together | ConsumerA (`customer_activities`), ConsumerB (`order_timeline`, `stock_levels`) via `ProcessAsync` |
| **Retried failures** recorded on the inbox row | ConsumerA rejects orders in `XXX` |
| **Inbox worker** for slow work, with backoff and dead-lettering | ConsumerB's welcome emails (`IngestAsync` + `IInboxHandler`); `@bounce.test` addresses fail |
| **Dead-letter admin API** | `GET/POST/DELETE /api/v1/admin/outbox/...` (`IOutboxAdministration`) |
| **Backlog health check** | `/health/ready` (`AddGMMessagingBacklogCheck`) |
| **Traces and metrics** | OpenTelemetry in every host (`AddSampleTelemetry`), exported to Jaeger |
| **One consumer name per service** | `Messaging:Inbox:ConsumerName`; handlers call `ProcessAsync(message, handler)` |
| **Retention** | Published outbox rows go after 7 days, handled inbox rows after 30 |

[docs/architecture.md](docs/architecture.md) walks one event through the system and lists what happens
on each kind of failure; [docs/operations.md](docs/operations.md) covers dead letters, health, traces and
settings.

## How GM.Messaging is wired

```csharp
// Producer persistence: outbox admin everywhere, the relay where asked, and the interceptor that wakes
// an in-process relay as soon as a save adds outbox messages.
services.AddDbContextPool<ApplicationDbContext>((sp, o) => o.UseNpgsql(cs).AddGMOutboxSignal(sp));
services.AddGMOutboxAdministration<OutboxMessage, ApplicationDbContext>();
if (runOutboxRelay)
    services.AddGMOutboxRelay<OutboxMessage, ApplicationDbContext>();

// Consumers: the inbox, and Wolverine taking ConsumerDbContext from the message's scope so handlers share
// it with IInboxProcessor (Wolverine can't build a DbContext in generated code).
builder.Services.AddInfrastructure(builder.Configuration, w => w.CodeGeneration.AlwaysUseServiceLocationFor<ConsumerDbContext>());
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

The `outbox_messages` and `inbox_messages` tables live in a shared `messaging` schema. Each service applies
its migrations, and creates its database, when it starts.

## Testing

```bash
dotnet test
```

Unit tests cover the command validators, the events (stable names, ordering keys, the version-2 upcaster,
trace capture) and outbox serialization. CI's `compose` job also runs the whole stack from
`docker-compose.yml` and drives it through the API: it checks that each consumer handled every event once,
that ConsumerB's timeline has an order before its payment, that the failing order was retried and the
bouncing email dead-lettered, and that the admin API and backlog check report a healthy outbox.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker, for `docker-compose.yml` (PostgreSQL, RabbitMQ, Jaeger and the four services).

## License

MIT — see [LICENSE](LICENSE).
