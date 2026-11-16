#!/bin/sh
# One RabbitMQ user per service, each limited to the exchanges and queues it uses (configure, write, read
# patterns). Runs once RabbitMQ is up, through the management API with the administrator account.
set -e
api="http://rabbitmq:15672/api"
auth="$RABBITMQ_ADMIN_USER:$RABBITMQ_ADMIN_PASSWORD"

user() { # name password
  curl -sf -u "$auth" -X PUT -H 'content-type: application/json' "$api/users/$1" \
    -d "{\"password\":\"$2\",\"tags\":\"\"}" > /dev/null
}
permissions() { # name configure write read
  curl -sf -u "$auth" -X PUT -H 'content-type: application/json' "$api/permissions/%2F/$1" \
    -d "{\"configure\":\"$2\",\"write\":\"$3\",\"read\":\"$4\"}" > /dev/null
}

# Shared names: the events exchange, the unrouted alternate exchange and queue, the dead-letter queue,
# server-named temporary queues and Wolverine's own.
shared='gm.events|gm.unrouted|gm.dead-letters|amq.gen-.*|wolverine.*'

# The API only saves outbox rows and wakes the relays: it may declare and publish to the wake-up exchange.
user gm-api "$GM_API_PASSWORD"
permissions gm-api '^gm.outbox.wake-up$' '^gm.outbox.wake-up$' '^$'

# The producer workers publish events, listen for wake-ups, and run the broker admin tools.
user gm-producer "$GM_PRODUCER_PASSWORD"
permissions gm-producer "^($shared|gm.outbox.wake-up)$" "^($shared|gm.outbox.wake-up|amq.default)$" "^($shared|gm.outbox.wake-up)$"

# Each consumer reaches its own queues, not the other consumer's.
for consumer in a b; do
  user "gm-consumer-$consumer" "$(eval echo \$GM_CONSUMER_$(echo $consumer | tr a-z A-Z)_PASSWORD)"
  permissions "gm-consumer-$consumer" "^($shared|gm.consumer-$consumer..*)$" "^($shared|gm.consumer-$consumer..*|amq.default)$" "^($shared|gm.consumer-$consumer..*)$"
done

echo "RabbitMQ users and permissions are set."
