#!/bin/bash

# Script to run database migrations for UberPrints
# Usage: ./scripts/migrate-database.sh [environment]
# environment: local (default) or docker

set -e

ENVIRONMENT="${1:-local}"

if [ "$ENVIRONMENT" = "docker" ]; then
    echo "Running migrations for Docker environment..."

    # Check if .env file exists
    if [ ! -f .env ]; then
        echo "Error: .env file not found"
        echo "Please create .env from .env.example and configure it"
        exit 1
    fi

    # Load password from .env
    export $(grep POSTGRES_PASSWORD .env | xargs)

    CONNECTION_STRING="Host=localhost;Database=uberprints;Username=postgres;Password=${POSTGRES_PASSWORD};Port=5432"

    echo "Using connection string: Host=localhost;Database=uberprints;Username=postgres;Password=***;Port=5432"

else
    echo "Running migrations for local development environment..."
    CONNECTION_STRING="Host=localhost;Database=uberprints;Username=postgres;Password=password"
fi

cd src/UberPrints.Server

echo "Applying migrations..."
dotnet ef database update --connection "$CONNECTION_STRING"

echo "✓ Database migrations applied successfully"
