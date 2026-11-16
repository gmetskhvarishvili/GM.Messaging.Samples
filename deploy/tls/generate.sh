#!/bin/sh
# Generates a private CA and a certificate for the RabbitMQ container (names: rabbitmq, localhost) into
# /certs, once. docker compose runs it before RabbitMQ starts; the services trust ca.pem
# (Messaging:RabbitMq:TlsCaCertificatePath). In production, use certificates from your own CA.
set -e
dir=/certs
if [ -f "$dir/server.pem" ]; then
  echo "TLS certificates already exist."
  exit 0
fi
apk add --no-cache openssl > /dev/null
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
rm -f "$dir/server.csr" "$dir/server.ext" "$dir/ca.srl"
# Readable by the RabbitMQ and service containers (sample only; keep keys private in production).
chmod 644 "$dir"/*
echo "Generated a sample CA and a certificate for rabbitmq/localhost."
