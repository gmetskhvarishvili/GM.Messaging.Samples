# Operations

## Run it

```bash
docker compose up -d --build
```

| URL | What |
| --- | --- |
| http://localhost:8080/swagger | API |
| http://localhost:8080/health/ready | API readiness: database + outbox backlog |
| http://localhost:8081/health/ready, :8082, :8083 | Consumer A, consumer B, producer worker readiness: database, RabbitMQ, backlog |
| http://localhost:8080…8084/admin/messaging/... | GM.Messaging's admin endpoints on every host (outbox, inbox, broker queues) |
| http://localhost:15672 | RabbitMQ management (gm-admin / gm-admin-secret) |
| http://localhost:16686 | Jaeger traces |
| http://localhost:9090 | Prometheus metrics |

`GM.Messaging.Sample.API/GM.Messaging.Sample.API.http` has a request for every endpoint, including the
failure cases and the workers' admin endpoints.

## Admin endpoints

Every host calls `MapGMMessagingAdmin()` (GM.Messaging.AspNetCore), which maps endpoints under
`/admin/messaging` for whatever that host registered:

| Host | Endpoints |
| --- | --- |
| API (8080) | `outbox` |
| Producer workers (8083, 8084) | `outbox`, `broker/dead-letters`, `broker/unrouted` |
| ConsumerA (8081), ConsumerB (8082) | `inbox`, `broker/dead-letters`, `broker/unrouted` |

They require the `messaging-admin` authorization policy. The sample has no identity provider, so that
policy accepts an API key in the `X-Admin-Key` header (`Admin:ApiKey`; docker-compose sets
`sample-admin-key`, appsettings has `local-dev-admin-key` for `dotnet run`). Without the key a request
gets 401. In a real service, point the policy at your own authentication (JWT, mTLS, ...). The examples
below use:

```bash
ADMIN="X-Admin-Key: sample-admin-key"
```

## Dead letters

There are three places a message can end up after its last attempt, each with its own endpoints.

**Outbox** (events the relay couldn't publish), through the API (`IOutboxAdministration`):

```bash
curl -H "$ADMIN" http://localhost:8080/admin/messaging/outbox                      # backlog
curl -H "$ADMIN" http://localhost:8080/admin/messaging/outbox/dead-letters         # what failed, and why
curl -H "$ADMIN" -X POST http://localhost:8080/admin/messaging/outbox/dead-letters/requeue   # retry all, after fixing the cause
```

**Inbox** (events ConsumerB's inbox worker gave up on), through ConsumerB (`IInboxAdministration`):

```bash
curl -H "$ADMIN" http://localhost:8082/admin/messaging/inbox                              # backlog
curl -H "$ADMIN" http://localhost:8082/admin/messaging/inbox/dead-letters                 # what failed, and why
curl -H "$ADMIN" -X POST http://localhost:8082/admin/messaging/inbox/dead-letters/requeue # retry all
```

**The broker's dead-letter queue** `gm.dead-letters` (messages Wolverine moved there after a consumer's
last retry, such as ConsumerA's XXX order), through ConsumerB (`IBrokerQueueAdministration`):

```bash
curl -H "$ADMIN" http://localhost:8082/admin/messaging/broker/dead-letters                # how many
curl -H "$ADMIN" "http://localhost:8082/admin/messaging/broker/dead-letters/messages?contains=XXX"  # look without consuming: original queue, reason (bodies hidden)
curl -H "$ADMIN" -X POST http://localhost:8082/admin/messaging/broker/dead-letters/replay # back to each message's original queue
curl -H "$ADMIN" -X POST "http://localhost:8082/admin/messaging/broker/dead-letters/replay?queue=gm.consumer-a.orders&dryRun=true"  # only count
```

The replay, move and messages endpoints take `type`, `queue` (the original queue), `routingKey`, `max`
and `dryRun`; messages that don't match stay in place, in order. ConsumerA's inbox row for the XXX order
is dead-lettered as well once the broker's last delivery fails, so its readiness reports the dead letter
(Degraded). Replaying the message from the broker hands it to the handler again.

### Recovering the XXX order, end to end

CI walks through this after every run; you can do the same on a local stack:

```bash
# 1. Fix the cause. Here: tell ConsumerA to accept XXX (a stand-in for a deployed fix).
curl -H "$ADMIN" -X POST http://localhost:8081/admin/sample/accept-failing-currency
# 2. Replay the order from the broker's dead-letter queue to ConsumerA's queue.
curl -H "$ADMIN" -X POST "http://localhost:8082/admin/messaging/broker/dead-letters/replay?queue=gm.consumer-a.orders"
# 3. The handler runs again: the inbox row is processed (no longer dead-lettered), the activity is recorded,
#    and the dead-letter queue is one message shorter.
curl -H "$ADMIN" http://localhost:8081/admin/messaging/inbox/dead-letters
```

## Unrouted messages

Every exchange GM.Messaging declares has the alternate exchange `gm.unrouted`, so a message published
where no queue is bound for it (a consumer that hasn't started yet, a wrong routing key) is kept in the
`gm.unrouted` queue instead of being dropped. Once the consumer's queue exists:

```bash
curl -H "$ADMIN" http://localhost:8082/admin/messaging/broker/unrouted                                 # how many
curl -H "$ADMIN" -X POST "http://localhost:8082/admin/messaging/broker/unrouted/move?exchange=gm.events" # publish them again
```

CI shows this with a `returns.requested` event that nothing consumes yet: it lands in `gm.unrouted`, then
a `gm.returns` queue bound to `returns.#` is created, and the move delivers it there.

## Old event versions on the wire

A producer that hasn't upgraded still publishes `InventoryUpdated` version 1 (`Warehouse`, no
`WarehouseCode`) with the `gm-event-version: 1` header. ConsumerB upcasts it before the handler runs. CI
publishes such a message straight to `gm.events` through the RabbitMQ management API and checks that
the `LEGACY-1` warehouse's stock level is recorded.

An existing RabbitMQ that already has the `gm.events` exchange without that argument refuses to redeclare
it: delete the exchange once (the services recreate it), or set `Messaging:RabbitMq:UnroutedExchange` to `""`.

## Security

**The broker.** RabbitMQ accepts only TLS (port 5671; the plain AMQP port is closed). On first start
`deploy/tls/generate.sh` creates a sample CA and a certificate for `rabbitmq` and `localhost` in
`deploy/tls/certs` (git-ignored), and the services trust that CA through
`Messaging:RabbitMq:TlsCaCertificatePath`. Each service has its own RabbitMQ user, created by
`deploy/rabbitmq/setup.sh`, limited to what it uses:

| User | May use |
| --- | --- |
| `gm-api` | publish to the wake-up exchange; reads no queue |
| `gm-producer` | the events exchange, the unrouted and dead-letter queues, wake-ups |
| `gm-consumer-a`, `gm-consumer-b` | the events exchange, the unrouted and dead-letter queues, and their own queues only |

The administrator (`gm-admin`) is for the management UI. In production, the certificates come from your
CA, passwords from a secret store, the management UI stays off public networks, and the broker's disks are
encrypted.

**Personal data in events.** Events carry ids where that's enough. `UserRegistered` needs the user's email
and name (for the welcome email and the activity feed), so they're `[SensitiveData]`: GM.Messaging encrypts
them with the key ring in `Messaging:SensitiveData` wherever the event is written (outbox and inbox rows,
messages on RabbitMQ, dead letters), and handlers get the plain values. Every host has the same key ring;
to rotate, add a key, make it current, and keep the old one while old messages may still be read. For
erasure requests, GM.Messaging can also encrypt each person's data with their own key and delete it
(`[DataSubject]`, `AddGMSubjectKeys`); this sample doesn't use that.

## Health

Every host maps `/health/live`, `/health/ready` and `/health/startup`. Readiness checks the database,
RabbitMQ (the workers) and `gm:messaging-backlog` (GM.HealthChecks.Messaging): the outbox in the API and
producer worker, the inbox in the consumers. The backlog check reports `Degraded`, not `Unhealthy`, when
more than 1000 messages wait, the oldest has waited over 2 minutes, or messages are dead-lettered (more
than 10 in ConsumerB, which dead-letters one email on purpose): work is late, but the service still runs.

## Traces and metrics

Every host sends traces and metrics over OTLP to the OpenTelemetry collector (`OTEL_EXPORTER_OTLP_ENDPOINT`;
config in `deploy/otel-collector.yaml`), which forwards traces to Jaeger and exposes metrics to Prometheus
(`deploy/prometheus.yml`). Sources: `GM.Messaging` (outbox publish, inbox process, inbox worker) and
`Wolverine`. Metrics, in Prometheus's naming:

| Metric | What |
| --- | --- |
| `gm_messaging_outbox_published_total`, `_failed_total`, `_dead_lettered_total` | Relay publishes |
| `gm_messaging_outbox_pending`, `gm_messaging_inbox_pending` | Backlog, per process (`service_instance_id`) |
| `gm_messaging_outbox_lag_seconds` | Time from raising an event to publishing it (histogram) |
| `gm_messaging_inbox_processed_total`, `_duplicates_total`, `_failed_total`, `_dead_lettered_total` | Consumers |
| `gm_messaging_cleanup_deleted_total` | Retention |

For example, `sum by (service_name) (rate(gm_messaging_inbox_processed_total[1m]))` is each consumer's
throughput, and a non-zero `gm_messaging_inbox_duplicates_total` means events were delivered more than once.
Compose exports metrics every 5 seconds (`OTEL_METRIC_EXPORT_INTERVAL`); the default is a minute.

## Settings worth knowing

| Setting | Where | Sample value |
| --- | --- | --- |
| `Messaging:RabbitMq:PublisherConfirms` | producer worker | `true` |
| `Messaging:RabbitMq:UnroutedExchange` | every service (default) | `gm.unrouted` |
| `Messaging:OutboxRelay:PollingIntervalSeconds` | producer worker | 30 s: saves wake the relay at once, polling is the fallback |
| `Messaging:OutboxRelay:WakeUpExchange` | API and producer worker | `gm.outbox.wake-up` |
| `Messaging:OutboxRelay:MaxRetries`, `RetryBaseDelaySeconds`, `LeaseSeconds`, `RetentionDays` | producer worker | 5, 2 s, 60 s, 7 days |
| `Messaging:Inbox:ConsumerName` | consumers | `consumer-a`, `consumer-b` |
| `Messaging:Inbox:MaxAttempts`, `RetryBaseDelaySeconds` | ConsumerB (inbox worker) | 3, 2 s |
| `ConsumerTopology:Bindings[].Sequential` | ConsumerB, order timeline queue | `true` |
| `Messaging:Retry:MaxAttempts` | consumers (Wolverine retries) | 3 (A), 5 (B) |

## Scaling

The relay and the inbox worker lease rows before working on them, so both can run as several replicas:
`docker compose up -d --scale producer-worker=2`. CI runs two relays and checks in Prometheus that both
report metrics, that every event was published exactly once, and that no consumer saw a duplicate. Every
relay gets the API's wake-ups (each listens on its own temporary queue).
