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
curl -H "$ADMIN" "http://localhost:8082/admin/messaging/broker/dead-letters/messages?contains=XXX"  # look without consuming: original queue, reason, body ([SensitiveData] masked)
curl -H "$ADMIN" -X POST http://localhost:8082/admin/messaging/broker/dead-letters/replay # back to each message's original queue
curl -H "$ADMIN" -X POST "http://localhost:8082/admin/messaging/broker/dead-letters/replay?queue=gm.consumer-a.orders&dryRun=true"  # only count
```

The replay, move and messages endpoints take `type`, `queue` (the original queue), `routingKey`, `max`
and `dryRun`; messages that don't match stay in place, in order. ConsumerA's inbox row for the XXX order
is dead-lettered as well once the broker's last delivery fails, so its readiness reports the dead letter
(Degraded). Replaying the message from the broker hands it to the handler again.

Peeking and dry runs take messages and put them back. The dead-letter and unrouted queues are quorum queues
with no delivery limit (`x-delivery-limit: -1`, GM.Messaging 4.11), so peeking as often as you like loses
nothing. RabbitMQ 4 would otherwise drop a message returned 20 times. CI peeks 25 times to check it.

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

CI shows this with `ReturnRequested`, which the API publishes (`POST /api/v1/returns`, routing key
`returns.requested`) before any returns service exists. It lands in `gm.unrouted`, where the admin endpoints
show it with the contact email masked. Then a `gm.returns` queue bound to `returns.#` is created, and the
move delivers it there.

## Old event versions on the wire

A producer that hasn't upgraded still publishes `InventoryUpdated` version 1 (`Warehouse`, no
`WarehouseCode`) with the `gm-event-version: 1` header. ConsumerB upcasts it before the handler runs. CI
publishes such a message straight to `gm.events` through the RabbitMQ management API and checks that
the `LEGACY-1` warehouse's stock level is recorded.

An existing RabbitMQ that already has the `gm.events` exchange without that argument refuses to redeclare
it: delete the exchange once (the services recreate it), or set `Messaging:RabbitMq:UnroutedExchange` to `""`.

## Security

**The broker.** RabbitMQ accepts only TLS (port 5671; the plain AMQP port is closed). On first start
`deploy/tls/generate.sh` creates a sample CA, a certificate for `rabbitmq` and `localhost`, and a client
certificate for ConsumerA in `deploy/tls/certs` (git-ignored). The services trust that CA through
`Messaging:RabbitMq:TlsCaCertificatePath`. Each service has its own RabbitMQ user, created by
`deploy/rabbitmq/setup.sh`, limited to what it uses:

| User | Signs in with | May use |
| --- | --- | --- |
| `gm-api` | password | publish to the wake-up exchange; reads no queue |
| `gm-producer` | password | the events exchange, the unrouted and dead-letter queues, wake-ups |
| `gm-consumer-a` | client certificate (no password) | the events exchange, the unrouted and dead-letter queues, and its own queues only |
| `gm-consumer-b` | password | the same, for its own queues |

**Mutual TLS.** ConsumerA presents `consumer-a.pem` (CN=`gm-consumer-a`, signed by the sample CA) and signs
in with RabbitMQ's EXTERNAL mechanism, which takes the user from the certificate's common name:

```json
"RabbitMq": {
  "UserName": "gm-consumer-a",
  "TlsClientCertificatePath": "/tls/consumer-a.pem",
  "TlsClientKeyPath": "/tls/consumer-a.key",
  "UseExternalAuthentication": true
}
```

RabbitMQ verifies client certificates when they're offered (`ssl_options.verify = verify_peer`), enables
EXTERNAL (`rabbitmq_auth_mechanism_ssl` in `deploy/rabbitmq/enabled_plugins`), and still lets the other
services use passwords. To move every service to certificates, give each one its own certificate and set
`ssl_options.fail_if_no_peer_cert = true`.

The administrator (`gm-admin`) is for the management UI. In production, the certificates come from your
CA, passwords from a secret store, the management UI stays off public networks, and the broker's disks are
encrypted.

## Personal data and keys

Events carry ids where that's enough. `UserRegistered` needs the user's email and name (for the welcome
email and the activity feed), and `ReturnRequested` a contact email, so those are `[SensitiveData]`.
GM.Messaging encrypts them wherever the event is written (outbox and inbox rows, messages on RabbitMQ, dead
letters), and handlers get the plain values.

**A key per person.** Each of those events names its person with `[DataSubject]`:
`[DataSubject(nameof(UserId))]` on `UserRegistered` (an inherited property), and on
`ReturnRequested.CustomerId`. Their values are encrypted with that person's own key, wrapped by the key ring
in `Messaging:SensitiveData`. The keys live in the shared `messaging_keys` database. `AddGMOutbox` and
`AddGMInbox(configuration)` set it up in every host from `ConnectionStrings:SubjectKeys` (with the same Npgsql and
snake_case settings as each host's own DbContext), and GM.Messaging creates the database and its
`subject_keys` table at startup, so the sample has no DbContext or migration for it. The API creates a
person's key before writing their first event (`EnsureKeyAsync`).

**Erasure requests.** Deleting the person's key makes their data unreadable in every copy of every event:
the outbox, both inboxes, dead letters and backups. Handlers reading those events get `null` for the
encrypted fields.

```bash
curl -H "$ADMIN" -X DELETE http://localhost:8080/admin/messaging/subject-keys/<user id>   # 204; 404 when there's none
```

The request also reaches the copies services made in their own tables. `AddGMOutbox` registers
erasure events in the API, so the same call writes GM.Messaging's
`gm.subject-forgotten` event (routing key `subject.forgotten`) to its outbox. Each consumer handles it
through its inbox:

| Service | Its copy | On `gm.subject-forgotten` |
| --- | --- | --- |
| ConsumerA | the email in its activity feed (`customer_activities.reference`) | replaced with `[erased]`; the activity stays |
| ConsumerB | the welcome email's address (`welcome_emails`) | the row is deleted |

The event goes out on every request, a repeat included, so a retried erasure still reaches everyone, and
the handlers are idempotent. Running processes cache keys for up to `SubjectKeyCacheSeconds` (60).

**Keys outside configuration.** `sample-2026-12` is a `dp:` value, protected with ASP.NET Core Data
Protection. Only processes with the Data Protection keys in `deploy/dataprotection`, mounted read-only into
each container, can use it. GM.Messaging reads them through `Messaging:SensitiveData:DataProtection`
(`KeysPath`, `ApplicationName: GM.Messaging.Sample`) with its own Data Protection, so the hosts set up
nothing for it. In production, keep Data Protection's keys in a shared store protected by Azure Key Vault or
AWS KMS. Or implement `ISensitiveDataKeyRing` against your key service. To make the next key, ask any host:

```bash
curl -H "$ADMIN" -X POST http://localhost:8080/admin/messaging/sensitive-data/keys   # {"value":"dp:..."}: add it under Keys
```
**Rotating the key ring.** CI does this after its other checks:

```bash
SAMPLE_CURRENT_KEY_ID=sample-2026-12 docker compose up -d --scale producer-worker=2   # deploy with the new current key
curl -H "$ADMIN" -X POST http://localhost:8080/admin/messaging/subject-keys/rewrap       # how many keys were re-wrapped
```

New per-person keys are wrapped with `sample-2026-12` straight away, and the re-wrap moves the existing
ones. Run it again and it re-wraps none. Keep `sample-2026-11` while messages encrypted with it directly may
still be read, then remove it.

**Admin endpoints.** Broker message bodies are shown with the contract's `[SensitiveData]` masked
(`MessageBodies = BrokerMessageBodies.Redacted`): a dead-lettered order is readable in full, while a
`ReturnRequested` in `gm.unrouted` shows `"ContactEmail": "***"`.

## Health

Every host maps `/health/live`, `/health/ready` and `/health/startup`. Readiness checks the database,
RabbitMQ (the workers) and `gm:messaging-backlog` (GM.HealthChecks.Messaging): the outbox in the API and
producer worker, the inbox in the consumers. The backlog check reports `Degraded`, not `Unhealthy`, when
more than 1000 messages wait, the oldest has waited over 2 minutes, or messages are dead-lettered (more
than 10 in ConsumerB, which dead-letters one email on purpose): work is late, but the service still runs.

The workers also report `gm:messaging-broker-queues`: what waits on RabbitMQ itself, read by GM.Messaging every 30
seconds. It reports `Degraded` when a consumer queue is over 80% of its 100,000-message limit, or when the
dead-letter or unrouted queue holds anything. In CI, ConsumerB's check goes `Degraded` while the XXX order waits
in `gm.dead-letters`.

## Settings worth knowing

| Setting | Where | Sample value |
| --- | --- | --- |
| `Messaging:RabbitMq:PublisherConfirms` | producer worker | `true` |
| `Messaging:RabbitMq:UnroutedExchange` | every service (default) | `gm.unrouted` |
| `Messaging:RabbitMq:QueueType` | every service (the same value everywhere) | `Quorum`: the consumer, dead-letter and unrouted queues are quorum queues |
| `Messaging:RabbitMq:MaxQueueLength` | every service | `100000`: a full consumer queue refuses new messages (`reject-publish`), and the relay retries |
| `Messaging:RabbitMq:DeadLetterRetentionHours`, `UnroutedRetentionHours` | every service (the same values everywhere) | `336`: messages older than two weeks are dropped from those queues |
| `ConsumerTopology:PrefetchCount`, `MaxParallelMessages` | ConsumerB | `50` by default; the inventory queue `20` and `4` |
| `Messaging:LargeMessages:OffloadAboveBytes`, `FileSystemPath` | producer worker and consumers (docker-compose.yml) | `65536`, `/claim-checks` (the shared `gm-claim-checks` volume) |
| `Messaging:OutboxRelay:PollingIntervalSeconds` | producer worker | 30 s: saves wake the relay at once, polling is the fallback |
| `Messaging:OutboxRelay:WakeUpExchange` | API and producer worker | `gm.outbox.wake-up` |
| `Messaging:OutboxRelay:MaxRetries`, `RetryBaseDelaySeconds`, `LeaseSeconds`, `RetentionDays` | producer worker | 5, 2 s, 60 s, 7 days |
| `Messaging:Inbox:ConsumerName` | consumers | `consumer-a`, `consumer-b` |
| `Messaging:Inbox:MaxAttempts`, `RetryBaseDelaySeconds` | ConsumerB (inbox worker) | 3, 2 s |
| `ConsumerTopology:Bindings[].Sequential` | ConsumerB, order timeline queue | `true` |
| `Messaging:Retry:MaxAttempts` | consumers (Wolverine retries) | 3 (A), 5 (B) |

## Scaling

The relay and the inbox worker lease rows before working on them, so both can run as several replicas:
`docker compose up -d --scale producer-worker=2`. CI runs two relays and checks with RabbitMQ's management
API that both are connected and that every event was published exactly once. Every
relay gets the API's wake-ups (each listens on its own temporary queue).
