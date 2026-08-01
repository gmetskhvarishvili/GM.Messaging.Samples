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
│   ├── GM.Messaging.Sample.API             # Web API — accepts commands, writes events to the outbox
│   ├── GM.Messaging.Sample.Application      # CQRS commands + validators (Create Order/Payment/…)
│   ├── GM.Messaging.Sample.Domain           # Integration events + the outbox aggregate
│   ├── GM.Messaging.Sample.Persistence      # EF Core persistence (PostgreSQL)
│   ├── GM.Messaging.Sample.Common           # Shared resources
│   └── GM.Messaging.Sample.Producer.Worker  # Relays the outbox to RabbitMQ
├── ConsumerA (Domain / Infrastructure / Persistence / Worker)   # consumes events, own inbox + DB
├── ConsumerB (Domain / Infrastructure / Persistence / Worker)   # a second, independent consumer
└── tests/
    └── GM.Messaging.Sample.Tests            # command-validator, event, and outbox unit tests
```

Only the published **GM.*** packages are referenced — no project references into the library repos.

## What it demonstrates

- **Transactional outbox** — commands write an `OutboxMessage` in the same transaction as their work;
  the producer worker relays unprocessed messages to RabbitMQ.
- **Inbox / idempotent consumers** — each consumer records what it has handled so redelivery is safe.
- **Two independent consumers** — ConsumerA and ConsumerB each have their own domain, persistence and
  worker, showing fan-out to separate services.
- **CQRS + validation** — mediator commands (`CreateOrder`, `ProcessPayment`, `RegisterUser`,
  `UpdateInventory`) with FluentValidation.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **RabbitMQ** and **PostgreSQL** — the [GM.Messaging repo's `docker-compose.yml`](https://github.com/gmetskhvarishvili/GM.Messaging)
  spins both up locally.

## Running

Start RabbitMQ + PostgreSQL, then run the producer API and the workers (separate terminals):

```bash
dotnet run --project GM.Messaging.Sample.API
dotnet run --project GM.Messaging.Sample.Producer.Worker
dotnet run --project GM.Messaging.Sample.ConsumerA.Worker
dotnet run --project GM.Messaging.Sample.ConsumerB.Worker
```

POST a command to the API (e.g. create an order); the event flows outbox → RabbitMQ → both consumers.

## Testing

```bash
dotnet test
```

The suite covers the command validators, the integration events, and outbox serialization — pure
unit tests that need no RabbitMQ or database.

## License

MIT — see [LICENSE](LICENSE).
