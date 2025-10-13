# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

UberPrints is a 3D print request management system where users can request 3D prints with optional delivery. The backend is an ASP.NET Core 10.0 Web API with PostgreSQL database, designed to eventually support Discord OAuth authentication.

## Build and Test Commands

### Building
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/UberPrints.Server/UberPrints.Server.csproj

# Restore dependencies
dotnet restore
```

### Running
```bash
# Run the API server
dotnet run --project src/UberPrints.Server/UberPrints.Server.csproj

# The API will be available at https://localhost:7001 (or http://localhost:5000)
# Scalar API documentation available at /scalar in development mode
```

### Testing
```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test test/UberPrints.Server.UnitTests/UberPrints.Server.UnitTests.csproj

# Run only integration tests (requires Docker for PostgreSQL container)
dotnet test test/UberPrints.Server.IntegrationTests/UberPrints.Server.IntegrationTests.csproj

# Run a single test class
dotnet test --filter FullyQualifiedName~RequestsControllerTests

# Run a specific test
dotnet test --filter FullyQualifiedName~RequestsControllerTests.CreateRequest_ReturnsCreatedRequest_WhenValidDataProvided
```

### Database Migrations
```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/UberPrints.Server

# Apply migrations to database
dotnet ef database update --project src/UberPrints.Server

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --project src/UberPrints.Server

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/UberPrints.Server
```

## Architecture

### High-Level Structure

The application follows a standard ASP.NET Core Web API architecture:

- **Controllers** (`Controllers/`): API endpoints grouped by resource
  - `RequestsController`: Public-facing print request management
  - `FilamentsController`: Public read-only filament catalog
  - `AdminController`: Admin-only operations for managing requests and filaments

- **Models** (`Models/`): Domain entities that map to database tables
  - Uses Entity Framework Core conventions
  - IDs are GUIDs generated with PostgreSQL's `uuidv7()` function
  - Enums stored as strings in database (see `RequestStatusEnum`)

- **DTOs** (`DTOs/`): Data Transfer Objects for API requests/responses
  - Separate create/update DTOs for input validation
  - Response DTOs include computed fields (e.g., `FilamentName` from join)

- **Data** (`Data/`): Entity Framework Core DbContext
  - `ApplicationDbContext`: Configures all entities and relationships

### Key Architectural Patterns

**Authentication Status**: The project has JWT Bearer authentication package installed but NOT yet implemented. Multiple controllers have `TODO` comments indicating where authentication checks should be added:
- `RequestsController.UpdateRequest` (line 128): needs ownership validation
- `RequestsController.DeleteRequest` (line 164): needs ownership validation
- `AdminController` (all endpoints): need admin role verification

**Guest Tracking**: Print requests can be created without authentication using a `GuestTrackingToken` for anonymous users to track their requests via `/api/requests/track/{token}`.

**Status History Pattern**: Print requests maintain an audit trail through `StatusHistory` entities. When status changes, a new history entry is created (see `AdminController.ChangeRequestStatus`).

**Include Pattern**: Controllers use EF Core's `.Include()` extensively to eager-load related entities and avoid N+1 queries. Always check existing patterns when adding new queries.

### Database Schema

The database uses PostgreSQL 18 with Entity Framework Core. Key relationships:

- `User` → many `PrintRequest` (optional, for authenticated users)
- `Filament` → many `PrintRequest` (required)
- `PrintRequest` → many `StatusHistory` (audit trail)
- `StatusHistory` → one `User` as `ChangedByUser` (optional, for admin tracking)

**Important**: IDs use `uuidv7()` for better database performance with ordered UUIDs. This is configured in `ApplicationDbContext.OnModelCreating`.

**Status Enum**: `RequestStatusEnum` includes: Pending, Accepted, Rejected, OnHold, Paused, WaitingForMaterials, Delivering, WaitingForPickup, Completed.

### Testing Strategy

The project uses xUnit with two test projects:

**Unit Tests** (`test/UberPrints.Server.UnitTests/`):
- Test individual controller methods with mocked dependencies
- Fast execution, no external dependencies

**Integration Tests** (`test/UberPrints.Server.IntegrationTests/`):
- Use `Microsoft.AspNetCore.Mvc.Testing` for full API testing
- Use Testcontainers.PostgreSql for isolated database per test run
- Inherit from `IntegrationTestBase` which provides test factory and HTTP client
- Tests demonstrate full request/response cycles including database operations

When writing new tests, follow the existing patterns in controller test files. Integration tests use `TestDataFactory` helper for creating test DTOs.

## Development Workflow

### Adding New Endpoints

1. Create/update DTOs in `DTOs/` folder
2. Add endpoint to appropriate controller
3. Use `.Include()` for any related entities needed
4. Check if authentication/authorization should be required (currently not enforced)
5. Add unit tests and integration tests following existing patterns

### Database Changes

1. Modify model classes in `Models/`
2. Update `ApplicationDbContext` if adding new DbSets or relationships
3. Create migration: `dotnet ef migrations add DescriptiveName --project src/UberPrints.Server`
4. Review generated migration code
5. Apply migration: `dotnet ef database update --project src/UberPrints.Server`

### Future Authentication Implementation

When implementing Discord OAuth + JWT:
- Configure authentication in `Program.cs` (package already installed)
- Add `[Authorize]` attributes to controllers/actions
- Add `[Authorize(Roles = "Admin")]` for admin endpoints
- Populate `User.DiscordId` during OAuth flow
- Set `PrintRequest.UserId` for authenticated users
- Set `StatusHistory.ChangedByUserId` for admin actions

## Configuration

Connection string is expected in `appsettings.json` or environment variables:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=uberprints;Username=postgres;Password=password"
}
```

For local development with Docker:
```bash
docker run --name uberprints-db \
  -e POSTGRES_PASSWORD=password \
  -e POSTGRES_DB=uberprints \
  -p 5432:5432 \
  -d postgres:18
```

## API Documentation

When running in development mode, Scalar API documentation is available at `/scalar`. The OpenAPI spec is available at `/openapi/v1.json`.
