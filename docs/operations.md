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
| http://localhost:15672 | RabbitMQ management (guest/guest) |
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
curl -H "$ADMIN" http://localhost:8082/admin/messaging/broker/dead-letters/messages       # look without consuming: original queue, reason, body
curl -H "$ADMIN" -X POST http://localhost:8082/admin/messaging/broker/dead-letters/replay # back to each message's original queue
curl -H "$ADMIN" -X POST "http://localhost:8082/admin/messaging/broker/dead-letters/replay?queue=gm.consumer-a.orders&dryRun=true"  # only count
```

The replay, move and messages endpoints take `type`, `queue` (the original queue), `routingKey`, `max`
and `dryRun`; messages that don't match stay in place, in order. ConsumerA's inbox row for the XXX order
is dead-lettered as well once the broker's last delivery fails, so its readiness reports the dead letter
(Degraded). Replaying the message from the broker hands it to the handler again.

## Unrouted messages

Every exchange GM.Messaging declares has the alternate exchange `gm.unrouted`, so a message published
where no queue is bound for it (a consumer that hasn't started yet, a wrong routing key) is kept in the
`gm.unrouted` queue instead of being dropped. Once the consumer's queue exists:

```bash
curl -H "$ADMIN" http://localhost:8082/admin/messaging/broker/unrouted                                 # how many
curl -H "$ADMIN" -X POST "http://localhost:8082/admin/messaging/broker/unrouted/move?exchange=gm.events" # publish them again
```

An existing RabbitMQ that already has the `gm.events` exchange without that argument refuses to redeclare
it: delete the exchange once (the services recreate it), or set `Messaging:RabbitMq:UnroutedExchange` to `""`.

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
