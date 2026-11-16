# Operations

## Run it

```bash
docker compose up -d --build
```

| URL | What |
| --- | --- |
| http://localhost:8080/swagger | API |
| http://localhost:8080/health/ready | Readiness: database + outbox backlog |
| http://localhost:15672 | RabbitMQ management (guest/guest) |
| http://localhost:16686 | Jaeger traces |

`GM.Messaging.Sample.API/GM.Messaging.Sample.API.http` has a request for every endpoint, including the
failure cases.

## Dead letters

**Outbox** (events the relay couldn't publish), through the API:

```bash
curl http://localhost:8080/api/v1/admin/outbox                      # backlog
curl http://localhost:8080/api/v1/admin/outbox/dead-letters         # what failed, and why
curl -X POST http://localhost:8080/api/v1/admin/outbox/dead-letters/requeue   # retry all, after fixing the cause
```

**Inbox** (events ConsumerB's inbox worker gave up on), in its database:

```sql
-- messaging_consumer_b
select event_id, consumer_name, event_type, attempts, error, dead_lettered_at_utc
from messaging.inbox_messages where dead_lettered_at_utc is not null;
```

Services can requeue them in code with `IInboxAdministration.RequeueAsync(eventId, consumerName)` (or
`RequeueAllDeadLetteredAsync`); ConsumerB is a worker without HTTP, so the sample leaves that to SQL or a
small admin tool.

## Health

The API's `/health/ready` includes `gm:messaging-backlog` (GM.HealthChecks.Messaging). It reports
`Degraded`, not `Unhealthy`, when more than 1000 messages wait, the oldest has waited over 2 minutes, or any
message is dead-lettered: work is late, but the API still serves requests.

## Traces and metrics

Every host sends traces and metrics over OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (compose points it
at Jaeger). Sources: `GM.Messaging` (outbox publish, inbox process, inbox worker) and `Wolverine`. Metrics
under `gm.messaging.*`: published, failed and dead-lettered messages, pending counts, publish lag,
handled and duplicate inbox messages, cleanup.

## Settings worth knowing

| Setting | Where | Sample value |
| --- | --- | --- |
| `Messaging:RabbitMq:PublisherConfirms` | producer worker | `true` |
| `Messaging:OutboxRelay:MaxRetries`, `RetryBaseDelaySeconds`, `LeaseSeconds`, `RetentionDays` | producer worker | 5, 2 s, 60 s, 7 days |
| `Messaging:Inbox:ConsumerName` | consumers | `consumer-a`, `consumer-b` |
| `Messaging:Inbox:MaxAttempts`, `RetryBaseDelaySeconds` | ConsumerB (inbox worker) | 3, 2 s |
| `ConsumerTopology:Bindings[].Sequential` | ConsumerB, order timeline queue | `true` |
| `Messaging:Retry:MaxAttempts` | consumers (Wolverine retries) | 3 (A), 5 (B) |

## Scaling

The relay and the inbox worker lease rows before working on them, so both can run as several replicas:
`docker compose up -d --scale producer-worker=2`.
