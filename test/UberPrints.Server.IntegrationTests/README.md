# UberPrints Server Integration Tests

This project contains integration tests for the UberPrints.Server API using xUnit, WebApplicationFactory, and TestContainers.

## Prerequisites

- .NET 10.0 SDK (or .NET 9.0)
- Docker Desktop (required for TestContainers to spin up PostgreSQL)

## Test Structure

The tests are organized by controller:

- **RequestsControllerTests.cs** - Tests for public request endpoints
  - GET /api/requests
  - GET /api/requests/{id}
  - GET /api/requests/track/{token}
  - POST /api/requests
  - PUT /api/requests/{id}
  - DELETE /api/requests/{id}

- **FilamentsControllerTests.cs** - Tests for public filament endpoints
  - GET /api/filaments
  - GET /api/filaments?inStock=true
  - GET /api/filaments/{id}

- **AdminControllerTests.cs** - Tests for admin endpoints
  - GET /api/admin/requests
  - PUT /api/admin/requests/{id}/status
  - POST /api/admin/filaments
  - PUT /api/admin/filaments/{id}
  - PATCH /api/admin/filaments/{id}/stock
  - DELETE /api/admin/filaments/{id}
  - GET /api/admin/filaments/{id}

## Test Coverage

Each test suite includes:
- ✅ **Happy path tests** - Verifying successful operations
- ✅ **Validation tests** - Testing error cases and constraints
- ✅ **Edge cases** - Out of stock filaments, invalid IDs, etc.

## Running the Tests

### Run all tests:
```bash
cd test/UberPrints.Server.IntegrationTests
dotnet test
```

### Run tests with detailed output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run a specific test class:
```bash
dotnet test --filter "FullyQualifiedName~RequestsControllerTests"
```

### Run a specific test:
```bash
dotnet test --filter "FullyQualifiedName~RequestsControllerTests.CreateRequest_ReturnsCreatedRequest_WhenValidDataProvided"
```

## Test Infrastructure

### IntegrationTestBase
Base class providing:
- HttpClient for making API requests
- Automatic database reset between tests
- Access to the test factory

### IntegrationTestFactory
WebApplicationFactory configuration providing:
- Shared PostgreSQL container using TestContainers
- Automatic database migrations
- Test environment configuration
- Database cleanup between tests

## Database Management

- **Container Lifecycle**: Single PostgreSQL container is shared across all tests
- **Data Isolation**: Database is truncated between each test
- **Migrations**: Automatically applied during test initialization
- **Container Image**: postgres:18

## Notes

- Tests use a shared PostgreSQL container for performance
- Database state is reset between tests using TRUNCATE statements
- The PostgreSQL container is automatically started and stopped
- First test run may be slower due to container image download
- Ensure Docker Desktop is running before executing tests

## Troubleshooting

If tests fail to start:
1. Ensure Docker Desktop is running
2. Check that port 5432 is not already in use
3. Try pulling the postgres image manually: `docker pull postgres:18`
4. Clear Docker resources: `docker system prune -a`
