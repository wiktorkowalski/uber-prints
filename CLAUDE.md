# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

UberPrints is a 3D print request management system where users can request 3D prints with optional delivery, featuring live printer monitoring and camera streaming. The system consists of:
- **Backend**: ASP.NET Core 10.0 Web API with PostgreSQL database, Discord OAuth authentication, and Prusa Link integration
- **Frontend**: React + Vite + TypeScript SPA with Tailwind CSS and shadcn/ui components
- **Printer Integration**: Real-time monitoring via Prusa Link API with background polling service
- **Camera Streaming**: RTSP-to-HLS live view with DVR buffer for rewinding

### Key Technologies & Dependencies

**Backend:**
- ASP.NET Core 10.0 with minimal APIs and OpenAPI/Scalar documentation
- Entity Framework Core 10.0 with PostgreSQL provider
- Background services (`IHostedService`) for printer monitoring and camera streaming
- FFmpeg for RTSP-to-HLS video conversion (external dependency)

**Frontend:**
- Vite 6.x for fast builds and HMR
- React 19 with TypeScript
- TanStack Router for routing
- HLS.js for video playback
- Axios for API communication
- React Hook Form + Zod for form validation
- shadcn/ui component library (Radix UI primitives + Tailwind CSS)

**Infrastructure:**
- Docker & Docker Compose for containerization
- GitHub Actions for CI/CD
- Cloudflare Tunnel for secure public access (optional)
- Watchtower for automated deployments (optional)

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

# Run the frontend dev server (from src/UberPrints.Client/)
cd src/UberPrints.Client
npm install  # First time only
npm run dev  # Starts Vite dev server on http://localhost:5173

# Build frontend for production (outputs to ../UberPrints.Server/wwwroot/)
npm run build
```

### Deployment
```bash
# Build and run with Docker Compose (includes database, server, cloudflared)
docker compose up -d --build

# Run database migrations (for Docker deployment)
./scripts/migrate-database.sh docker

# View logs
docker compose logs -f server

# Stop services
docker compose down
```

See [DEPLOYMENT.md](./DEPLOYMENT.md) for complete deployment guide with Cloudflare Tunnel setup.

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
  - `FilamentRequestsController`: User-submitted requests for new filaments
  - `AdminController`: Admin-only operations for managing requests and filaments
  - `AuthController`: Discord OAuth authentication, JWT token generation, and guest session management
  - `PrintersController`: Admin endpoints for printer control (upload GCode, pause/resume/cancel, test connection, get snapshot)
  - `PrinterStatusController`: Public endpoints for printer status and print queue monitoring
  - `StreamController`: Camera streaming control (start/stop, viewer management, buffer diagnostics)
  - `ProfileController`: User profile management (display name, Discord info)
  - `ThumbnailController`: Fetches Open Graph thumbnails from 3D model URLs

- **Models** (`Models/`): Domain entities that map to database tables
  - Uses Entity Framework Core conventions
  - IDs are GUIDs generated with PostgreSQL's `uuidv7()` function
  - Enums stored as strings in database (see `RequestStatusEnum`)

- **DTOs** (`DTOs/`): Data Transfer Objects for API requests/responses
  - Separate create/update DTOs for input validation
  - Response DTOs include computed fields (e.g., `FilamentName` from join)

- **Data** (`Data/`): Entity Framework Core DbContext
  - `ApplicationDbContext`: Configures all entities and relationships

- **Services** (`Services/`): Background services and external integrations
  - `PrusaLinkClient`: HTTP client for communicating with Prusa Link API
  - `PrinterMonitoringService`: Background service that polls printer status every 5-30 seconds (adaptive polling based on printer state)
  - `CameraStreamingService`: Background service managing FFmpeg for RTSP-to-HLS conversion with DVR buffer
  - `ThermalPrinterService`: Prints receipt-style tickets for new print requests via external thermal printer API
  - `DiscordService`: Sends Discord DM notifications for new requests and status changes
  - `ChangeTrackingService`: Tracks and audits changes to print request fields
  - `StreamStateService`: In-memory management of camera streaming state and viewer sessions

### Key Architectural Patterns

**Authentication**: The system uses Discord OAuth with JWT tokens and cookie-based authentication:
- Discord OAuth flow handled by `AuthController` with callback at `/api/auth/discord/callback`
- JWT tokens generated after successful OAuth, returned to frontend via redirect
- Cookie authentication for session persistence (30-day expiration)
- Guest sessions supported via `GuestSessionToken` on User model
- Authentication configured in `Program.cs` with both JWT Bearer and Cookie schemes
- Admin authorization enforced via `[Authorize(Roles = "Admin")]` on `AdminController` and admin endpoints
- Request ownership validation implemented in `RequestsController.UpdateRequest` and `DeleteRequest`

**Guest Session Flow**:
- Anonymous users can create guest sessions via `/api/auth/guest` to get a `GuestSessionToken`
- Guest users can be converted to authenticated users when they log in via Discord
- Guest print requests are linked via `UserId` relationship

**Guest Tracking**: Print requests have a separate `GuestTrackingToken` (distinct from guest session) for anonymous tracking via `/api/requests/track/{token}` without authentication.

**Status History Pattern**: Print requests maintain an audit trail through `StatusHistory` entities. When status changes, a new history entry is created (see `AdminController.ChangeRequestStatus`).

**Include Pattern**: Controllers use EF Core's `.Include()` extensively to eager-load related entities and avoid N+1 queries. Always check existing patterns when adding new queries.

**Prusa Link Integration**: Real-time printer monitoring and control:
- `PrusaLinkClient` service communicates with Prusa printers via HTTP API
- `PrinterMonitoringService` polls printer status at adaptive intervals (5s when printing, 30s when idle)
- Printer configuration stored in `Printers` table (IP address, API key, location)
- Status data cached in database: temperatures, print progress, time remaining, current file
- `PrinterStatusHistory` maintains audit trail of printer state changes
- Admin endpoints support GCode upload, print control (pause/resume/cancel), and camera snapshots

**Camera Streaming**: Live RTSP-to-HLS conversion with DVR functionality:
- `CameraStreamingService` uses FFmpeg to convert RTSP camera feed to HLS format
- DVR buffer (configurable 5-240 minutes, default 30) allows rewinding live stream
- Viewer tracking system: start stream when first viewer joins, stop when last viewer leaves
- Heartbeat mechanism keeps viewer sessions alive (10-second interval)
- Admin controls for buffer management (trim old segments, reset buffer, configure duration)
- HLS segments stored in `wwwroot/stream/` directory, served as static files

**Thermal Printer Integration**: Automatic receipt printing for new print requests:
- `ThermalPrinterService` calls external thermal printer API at `https://printer.vicio.ovh/api/printer/custom`
- Fire-and-forget pattern: failures don't block request creation
- Prints receipt with request ID, requester name, filament details, QR code linking to request page
- Triggered automatically on new print request creation

**Discord Notifications**: DM notifications for admins and requesters:
- `DiscordService` sends Discord DMs using bot token
- Admin notifications: All admin users notified when new print request created
- Requester notifications: Users notified on status changes (opt-in via `NotifyOnStatusChange` field, defaults to true)
- Fire-and-forget pattern: notification failures don't block operations
- Requires Discord bot token and users must have Discord authentication

**Filament Request System**: Users can request new filaments to be added:
- Separate workflow from print requests with own status lifecycle (Pending → Approved/Rejected/Ordered/Received)
- Supports both authenticated and guest users
- Users can view/delete their own requests, admins can manage all requests and change status
- Full audit trail via `FilamentRequestStatusHistory`

**Change Tracking**: Audit trail for print request modifications:
- `ChangeTrackingService` tracks changes to key fields: RequesterName, ModelUrl, Notes, RequestDelivery, IsPublic, FilamentId
- Stores old/new values, timestamp, and user who made change
- Changes included in PrintRequestDto responses for full transparency
- Used by both user and admin update endpoints

### Database Schema

The database uses PostgreSQL 18 with Entity Framework Core. Key relationships:

- `User` → many `PrintRequest` (optional, for authenticated users)
- `User` → many `FilamentRequest` (optional, for authenticated users)
- `Filament` → many `PrintRequest` (optional)
- `PrintRequest` → many `StatusHistory` (audit trail)
- `PrintRequest` → many `PrintRequestChange` (change tracking audit trail)
- `StatusHistory` → one `User` as `ChangedByUser` (optional, for admin tracking)
- `FilamentRequest` → many `FilamentRequestStatusHistory` (audit trail)
- `Printer` → many `PrintRequest` (optional, for tracking which printer handled the request)
- `Printer` → many `PrinterStatusHistory` (audit trail of printer state changes)

**Important**: IDs use `uuidv7()` for better database performance with ordered UUIDs. This is configured in `ApplicationDbContext.OnModelCreating`.

**User Model**: Users can exist in two states:
- **Authenticated**: Has `DiscordId`, `Username`, `Email` populated after Discord OAuth
- **Guest**: Has only `GuestSessionToken` and auto-generated username like `Guest_12345678`
- A guest user can be converted to authenticated when they log in via Discord
- Unique indexes on both `DiscordId` and `GuestSessionToken` fields

**Status Enums**:
- `RequestStatusEnum`: Pending, Accepted, Rejected, OnHold, Paused, WaitingForMaterials, Delivering, WaitingForPickup, Completed
- `FilamentRequestStatusEnum`: Pending, Approved, Rejected, Ordered, Received

**Printer Model**: Stores 3D printer configuration and real-time status:
- Configuration: Name, IP address, API key, location, active status
- Real-time status: Current state (Unknown, Idle, Printing, Paused, Finished, Error, etc.)
- Temperature readings: Nozzle and bed temperatures (current and target)
- Print progress: Percentage complete, time remaining, time printing, current filename
- Status updates cached in database and refreshed by `PrinterMonitoringService`
- Default printer created automatically on first startup if none exists

### Testing Strategy

The project uses multiple testing approaches:

**Unit Tests** (`test/UberPrints.Server.UnitTests/`):
- Test individual controller methods with mocked dependencies
- Fast execution, no external dependencies
- Run with: `dotnet test test/UberPrints.Server.UnitTests`

**Integration Tests** (`test/UberPrints.Server.IntegrationTests/`):
- Use `Microsoft.AspNetCore.Mvc.Testing` for full API testing
- Use Testcontainers.PostgreSql for isolated database per test run
- Inherit from `IntegrationTestBase` which provides test factory and HTTP client
- Tests demonstrate full request/response cycles including database operations
- Run with: `dotnet test test/UberPrints.Server.IntegrationTests`

**End-to-End Tests** (`test/UberPrints.Client.Playwright/`):
- Use Playwright for browser-based E2E testing
- Test complete user workflows across frontend and backend
- Support multiple browsers (Chromium, Firefox, WebKit) and mobile devices
- Automatically start backend and frontend servers
- Setup: `cd test/UberPrints.Client.Playwright && npm install && npx playwright install`
- Run with: `npm test` (in Playwright directory)
- See `test/UberPrints.Client.Playwright/README.md` for detailed documentation

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

### Frontend Development

The React frontend (`src/UberPrints.Client/`) uses:
- **React Router** for navigation with protected routes (`ProtectedRoute.tsx`)
- **AuthContext** (`contexts/AuthContext.tsx`) for global auth state management
- **Axios** for API communication (configured in `lib/api.ts`)
- **shadcn/ui** components in `components/ui/` (built on Radix UI)
- **React Hook Form + Zod** for form handling and validation
- **Vite proxy** configuration to proxy `/api` requests to backend at `https://localhost:7001` (dev only)
- **Production build** outputs to `src/UberPrints.Server/wwwroot/` and is served by ASP.NET Core

In development, run frontend and backend separately (frontend uses Vite proxy for API calls).
In production, ASP.NET Core serves the built React app from wwwroot using `UseStaticFiles()` and `MapFallbackToFile()`.

Key frontend pages:
- `Home.tsx`: Landing page
- `NewRequest.tsx`: Form to create print requests
- `RequestList.tsx`: View all print requests
- `RequestDetail.tsx`: View single request details
- `Dashboard.tsx`: User dashboard
- `AdminDashboard.tsx`: Admin panel with printer management (protected)
- `PrinterStatus.tsx`: Real-time printer status and print queue monitoring (public)
- `LiveView.tsx`: Live camera stream with DVR playback (public, with admin controls)
- `AuthCallback.tsx`: Handles OAuth callback and JWT token storage

Key frontend components:
- `PrinterStatusCard.tsx`: Displays printer status, temperatures, and progress
- `TemperatureDisplay.tsx`: Shows nozzle/bed temperatures with gauges
- `PrintProgress.tsx`: Progress bar for active prints
- `VideoPlayer.tsx`: HLS video player with retry logic and error handling

When adding new features:
1. Create API client functions in `lib/api.ts`
2. Add new routes in `App.tsx`
3. Use existing UI components from `components/ui/`
4. Follow the existing patterns for auth-protected routes

## Configuration

### Environment Variables

The application uses **DotNetEnv** to load configuration from a `.env` file for local development. Secrets are loaded from environment variables, never committed to git.

**Setup for local development:**
```bash
# Copy the example file
cp .env.example .env

# Edit .env with your actual secrets
# Required variables:
# - DISCORD_CLIENT_ID
# - DISCORD_CLIENT_SECRET
# - DISCORD_BOT_TOKEN (for Discord DM notifications)
# - JWT_SECRET_KEY (minimum 32 characters)
# - POSTGRES_PASSWORD
# - PrusaLink__IpAddress (printer IP address)
# - PrusaLink__ApiKey (from printer: Settings → Prusa Connect → API Key)
# - Camera__RtspUrl (RTSP camera stream URL, e.g., rtsp://192.168.1.35/live)
```

**appsettings.json structure** (no secrets here):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=uberprints;Username=postgres;Password=password"
  },
  "Jwt": {
    "SecretKey": "",  // Loaded from JWT_SECRET_KEY env var
    "Issuer": "UberPrints",
    "Audience": "UberPrints",
    "ExpiryHours": "168"
  },
  "Discord": {
    "ClientId": "",  // Loaded from DISCORD_CLIENT_ID env var
    "ClientSecret": "",  // Loaded from DISCORD_CLIENT_SECRET env var
    "BotToken": ""  // Loaded from DISCORD_BOT_TOKEN env var (for DM notifications)
  },
  "Frontend": {
    "Url": "http://localhost:5173"
  },
  "PrusaLink": {
    "IpAddress": "",  // Loaded from PrusaLink__IpAddress env var
    "ApiKey": "",     // Loaded from PrusaLink__ApiKey env var
    "PollingIntervalIdle": 30,    // Poll every 30 seconds when idle
    "PollingIntervalActive": 5,   // Poll every 5 seconds when printing
    "RequestTimeout": 30,
    "MaxRetryAttempts": 3
  },
  "Camera": {
    "RtspUrl": "",               // Loaded from Camera__RtspUrl env var
    "HlsSegmentDuration": 2,     // 2-second HLS segments for low latency
    "MaxSegments": 6,            // Keep 6 segments in live buffer
    "OutputDirectory": "stream", // Output to wwwroot/stream/
    "ConnectionTimeoutSeconds": 10,
    "DvrBufferMinutes": 30       // 30-minute rewind buffer
  }
}
```

**Required for OAuth**: Discord OAuth application must be configured at https://discord.com/developers/applications with redirect URI: `https://localhost:7001/api/auth/discord/callback`

**Required for Discord Notifications**: Discord bot must be created at https://discord.com/developers/applications with bot token and DM permissions

**External Services**:
- Thermal printer API at `https://printer.vicio.ovh/api/printer/custom` (hardcoded, automatically called on new requests)

**How environment variables work:**
- Local development: `Program.cs` uses DotNetEnv to load `.env` file automatically
- Docker: Environment variables passed via `docker-compose.yml` from `.env` file
- Production: Environment variables set by hosting platform or docker-compose

**Configuration Validation:**
The application uses ASP.NET Core's Options pattern with data annotation validation:
- Configuration classes in `Configuration/` folder (DiscordOptions, JwtOptions, FrontendOptions, PrusaLinkOptions, CameraOptions)
- Validation attributes enforce required fields and constraints (e.g., JWT secret minimum 32 characters, camera buffer 5-240 minutes)
- `ValidateOnStart()` ensures invalid configuration is caught at startup, not runtime
- Application will fail to start with clear error messages if configuration is invalid or missing
- Example: If JWT SecretKey is less than 32 characters, you'll see: "JWT SecretKey must be at least 32 characters long"
- PrusaLink and Camera configuration are required for printer monitoring and streaming features

For local development with Docker:
```bash
docker run --name uberprints-db \
  -e POSTGRES_PASSWORD=password \
  -e POSTGRES_DB=uberprints \
  -p 5432:5432 \
  -d postgres:18
```

## Deployment Automation

The project supports automated continuous deployment:

**GitHub Actions CI/CD**:
- Automatically builds Docker images on push to `master` branch
- Pushes images to GitHub Container Registry (ghcr.io)
- Configured in `.github/workflows/` directory

**Watchtower Auto-Deployment** (optional):
- Monitors running Docker containers for new images
- Automatically updates and restarts containers when new builds are available
- Polls GHCR every 5 minutes by default
- See `WATCHTOWER.md` for detailed setup instructions
- Deployment flow: Push to GitHub → Actions builds → Watchtower deploys (within 5 minutes)

**Manual Deployment**:
- `docker compose pull` to pull latest images
- `docker compose up -d` to recreate containers with new images
- `./scripts/migrate-database.sh docker` to apply pending migrations

**Health Checks**:
- Server exposes `/health/ready` endpoint for container health monitoring
- Database has `pg_isready` health check
- Docker Compose waits for database health before starting server

## API Documentation

When running in development mode, Scalar API documentation is available at `/scalar`. The OpenAPI spec is available at `/openapi/v1.json`.
