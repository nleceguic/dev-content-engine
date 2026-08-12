#!/usr/bin/env bash
#
# Deploy script for the Ubuntu server. Pulls the latest published Worker/Api images (see the
# "image:" tags in docker-compose.yml — a CI pipeline is expected to build and push them to that
# registry before this script runs) and restarts the containers with the new images.
#
# Usage (from anywhere): ./scripts/deploy.sh

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

if [ ! -f .env ]; then
    echo "Error: .env no existe. Copia .env.example a .env y rellena los valores antes de desplegar." >&2
    exit 1
fi

echo "==> Pulling latest images"
docker compose pull

echo "==> Recreating containers with the new images"
docker compose up -d --remove-orphans

echo "==> Removing dangling images"
docker image prune -f

echo "==> Current status"
docker compose ps
