# Architecture

## Processes

| Process | Does | Database |
| --- | --- | --- |
| `GM.Messaging.Sample.API` | Accepts commands; each saves its integration event to the outbox in the same transaction, and wakes the relays over RabbitMQ. Applies the `messaging_api` migrations. Exposes the outbox admin API and health checks. | `messaging_api` |
| `GM.Messaging.Sample.Producer.Worker` (one or more) | Runs the outbox relay: publishes pending events to RabbitMQ, with confirms, leases, retries, dead-lettering and ordering keys. Waits for the API's migrations. Health checks. | `messaging_api` |
| `GM.Messaging.Sample.ConsumerA.Worker` | Records customer activity from orders, payments and registrations (`ProcessAsync`). | `messaging_consumer_a` |
| `GM.Messaging.Sample.ConsumerB.Worker` | Keeps an order timeline (sequential queue) and stock levels (`ProcessAsync`), and sends welcome emails through the inbox worker (`IngestAsync`). | `messaging_consumer_b` |

## One event, end to end

```mermaid
sequenceDiagram
    participant C as Client
    participant API
    participant DB as messaging_api
    participant R as Relay (producer worker)
    participant MQ as RabbitMQ
    participant A as ConsumerA
    participant B as ConsumerB

    C->>API: POST /api/v1/orders
    API->>DB: INSERT order event into outbox_messages (one transaction, with the trace id)
    API-->>R: wake-up over RabbitMQ (gm.outbox.wake-up); otherwise the relay polls every 30 s
    R->>DB: claim due messages (lease, compare-and-set; one per ordering key)
    R->>MQ: publish orders.order-placed (waits for the broker's confirm)
    R->>DB: mark published
    MQ-->>A: gm.consumer-a.orders
    A->>A: ProcessAsync: inbox row + customer_activities row, one transaction
    MQ-->>B: gm.consumer-b.order-timeline (sequential)
    B->>B: ProcessAsync: inbox row + order_timeline row, one transaction
```

The client's trace continues through every step (Jaeger, behind the OpenTelemetry collector, shows it as one trace): the outbox row stores the
W3C trace context, the relay publishes in it, and Wolverine carries it to the consumers.

## What happens when something fails

| Failure | What GM.Messaging does | Where to see it |
| --- | --- | --- |
| RabbitMQ is down or rejects a publish | The relay records the error, retries with backoff (`OutboxRelay:RetryBaseDelaySeconds`, doubling), and dead-letters after `MaxRetries`. | `GET /admin/messaging/outbox`, the readiness check (Degraded), relay logs |
| An event type has no route | Same as a failed publish, with a `NoRouteException` naming the missing `ProducerTopology` entry. | dead letters in the admin API |
| An event is routed to an exchange, but no queue is bound for it | RabbitMQ hands it to the alternate exchange `gm.unrouted`, which keeps it in the `gm.unrouted` queue. | ConsumerB `GET /admin/messaging/broker/unrouted`; move them back with `POST /admin/messaging/broker/unrouted/move` |
| A ConsumerA handler throws (orders in `XXX`) | The handler's changes roll back, the inbox row records the error and the attempt, and Wolverine retries (`Messaging:Retry`) before moving the message to `gm.dead-letters`; the last failed delivery dead-letters the inbox row too. | `messaging_consumer_a.messaging.inbox_messages`; ConsumerB `GET /admin/messaging/broker/dead-letters/messages`, then `POST .../replay` |
| The welcome email bounces (`@bounce.test`) | The inbox worker retries with backoff (`Messaging:Inbox`) and dead-letters the row after `MaxAttempts`. | ConsumerB `GET /admin/messaging/inbox/dead-letters` |
| The same event arrives twice | The inbox skips it: only one activity or timeline row per event (unique `event_id`). | the `gm.messaging.inbox.duplicates` metric |
| A relay stops mid-batch | Its leases expire after `LeaseSeconds` and another relay (or the restarted one) publishes those messages. | |

## Ordering

`OrderPlaced` and `PaymentProcessed` implement `IOrderedIntegrationEvent` with the order id as the key. The
relay publishes a key's events one at a time in the order they were raised, even with several relays, and
ConsumerB reads both from one queue marked `"Sequential": true`, so its timeline shows an order before its
payment. Events without a key, and different orders, don't wait for each other.

## Event versions

`InventoryUpdated` is at version 2 (`Warehouse` was renamed `WarehouseCode`). The producer worker and
ConsumerB register an upcaster with `AddGMIntegrationEvents` (`SampleEventVersions.InventoryUpdatedV1ToV2`):

- the relay converts outbox rows written at version 1 before publishing them;
- every published event carries its version in the `gm-event-version` header, so ConsumerB also converts
  version-1 events from a producer that hasn't been upgraded yet.
