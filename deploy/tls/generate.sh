#!/bin/sh
# Generates a private CA, a certificate for the RabbitMQ container (names: rabbitmq, localhost) and a client
# certificate for ConsumerA (CN=gm-consumer-a, mutual TLS) into /certs, each once. docker compose runs it
# before RabbitMQ starts; the services trust ca.pem (Messaging:RabbitMq:TlsCaCertificatePath). In production,
# use certificates from your own CA.
set -e
dir=/certs
if [ -f "$dir/server.pem" ] && [ -f "$dir/consumer-a.pem" ]; then
  echo "TLS certificates already exist."
  exit 0
fi
apk add --no-cache openssl > /dev/null

if [ ! -f "$dir/server.pem" ]; then
  openssl req -x509 -newkey rsa:2048 -nodes -days 825 -subj "/CN=GM Messaging Sample CA" \
    -keyout "$dir/ca.key" -out "$dir/ca.pem" 2> /dev/null
  openssl req -newkey rsa:2048 -nodes -subj "/CN=rabbitmq" -keyout "$dir/server.key" -out "$dir/server.csr" 2> /dev/null
  cat > "$dir/server.ext" <<EXT
subjectAltName=DNS:rabbitmq,DNS:localhost
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
EXT
  openssl x509 -req -in "$dir/server.csr" -CA "$dir/ca.pem" -CAkey "$dir/ca.key" -CAcreateserial \
    -days 825 -extfile "$dir/server.ext" -out "$dir/server.pem" 2> /dev/null
  echo "Generated a sample CA and a certificate for rabbitmq/localhost."
fi

# ConsumerA's client certificate: RabbitMQ signs it in as the user named by its common name (EXTERNAL).
openssl req -newkey rsa:2048 -nodes -subj "/CN=gm-consumer-a" -keyout "$dir/consumer-a.key" -out "$dir/consumer-a.csr" 2> /dev/null
cat > "$dir/client.ext" <<EXT
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=clientAuth
EXT
openssl x509 -req -in "$dir/consumer-a.csr" -CA "$dir/ca.pem" -CAkey "$dir/ca.key" -CAcreateserial \
  -days 825 -extfile "$dir/client.ext" -out "$dir/consumer-a.pem" 2> /dev/null
echo "Generated ConsumerA's client certificate (CN=gm-consumer-a)."

rm -f "$dir"/*.csr "$dir"/*.ext "$dir/ca.srl"
# Readable by the RabbitMQ and service containers (sample only; keep keys private in production).
chmod 644 "$dir"/*
