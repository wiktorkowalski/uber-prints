#!/bin/bash
set -e  # Exit on error

echo "🚀 Deploying UberPrints server..."

# Pull latest image
echo "📥 Pulling latest image from GHCR..."
docker compose -f docker-compose.ghcr.yml pull server

# Recreate server container
echo "🔄 Restarting server with new image..."
docker compose -f docker-compose.ghcr.yml up -d --no-deps --force-recreate server

# Wait for health check
echo "⏳ Waiting for server to be healthy..."
timeout 60 bash -c 'until [ "$(docker inspect --format="{{.State.Health.Status}}" uberprints-server 2>/dev/null)" = "healthy" ]; do sleep 2; done' || {
  echo "❌ Health check failed or timed out"
  docker compose -f docker-compose.ghcr.yml logs --tail=50 server
  exit 1
}

echo "✅ Deployment complete! Server is healthy."
docker compose -f docker-compose.ghcr.yml ps server
