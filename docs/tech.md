# UberPrints 3D Request System - Technical Specification

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 10.0 Web API
- **Language**: C# 12
- **Database**: PostgreSQL 18
- **ORM**: Entity Framework Core 8.0
- **Authentication**: Discord OAuth 2.0 + JWT tokens
- **Authorization**: Role-based access control (User/Admin)
- **API Documentation**: OpenAPI 3.0 (Swagger)

### Frontend
- **Framework**: React 18 with TypeScript
- **Build Tool**: Vite 5.0
- **UI Library**: shadcn/ui (built on Radix UI)
- **Styling**: Tailwind CSS
- **State Management**: React Context + useReducer
- **HTTP Client**: Axios
- **Routing**: React Router v6

### Development & Deployment
- **Containerization**: Docker
- **Orchestration**: Docker Compose
- **Environment**: .NET 10 SDK, Node.js 20+
- **Database Migrations**: Entity Framework Core Migrations

## Architecture Overview

```
┌─────────────────┐    HTTP/HTTPS    ┌─────────────────┐
│   React App     │ ◄──────────────► │  ASP.NET API    │
│  (Frontend)     │                  │   (Backend)     │
└─────────────────┘                  └─────────────────┘
                                              │
                                              │
                                              ▼
                                    ┌─────────────────┐
                                    │  PostgreSQL 18  │
                                    │    (Database)   │
                                    └─────────────────┘
```

### Key Architectural Patterns
- **RESTful API**: Clean separation between frontend and backend
- **Controller-based Architecture**: Controllers handle HTTP requests directly with EF Core
- **Service Layer**: Business logic for complex operations (e.g., `IChangeTrackingService`)
- **Dependency Injection**: ASP.NET Core built-in DI container
- **Dual Authentication**: JWT tokens for API + Cookies for session persistence
- **Options Pattern**: Strongly-typed configuration with validation
- **Include Pattern**: EF Core eager loading to avoid N+1 queries

## Database Schema

### Entity Models

#### User
```csharp
public class User
{
    public Guid Id { get; set; }
    public string? DiscordId { get; set; }
    public string? GuestSessionToken { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? GlobalName { get; set; }
    public string? AvatarHash { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PrintRequest> PrintRequests { get; set; } = new();
}
```

#### PrintRequest
```csharp
public class PrintRequest
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? GuestTrackingToken { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string ModelUrl { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool RequestDelivery { get; set; }
    public bool IsPublic { get; set; } = true;
    public Guid? FilamentId { get; set; }
    public RequestStatusEnum CurrentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public Filament? Filament { get; set; }
    public List<StatusHistory> StatusHistory { get; set; } = new();
    public List<PrintRequestChange> Changes { get; set; } = new();
}
```

#### RequestStatusEnum
```csharp
public enum RequestStatusEnum
{
    Pending,
    Accepted,
    Rejected,
    OnHold,
    Paused,
    WaitingForMaterials,
    Delivering,
    WaitingForPickup,
    Completed
}
```

#### StatusHistory
```csharp
public class StatusHistory
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public RequestStatusEnum Status { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime Timestamp { get; set; }

    public PrintRequest Request { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
```

#### Filament
```csharp
public class Filament
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public decimal StockAmount { get; set; }
    public string StockUnit { get; set; } = "grams";
    public string? Link { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<PrintRequest> PrintRequests { get; set; } = new();
    public List<FilamentRequest> FilamentRequests { get; set; } = new();
}
```

#### PrintRequestChange
```csharp
public class PrintRequestChange
{
    public Guid Id { get; set; }
    public Guid PrintRequestId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }

    public PrintRequest PrintRequest { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
```

#### FilamentRequest
```csharp
public class FilamentRequest
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string? Notes { get; set; }
    public FilamentRequestStatusEnum CurrentStatus { get; set; }
    public Guid? FilamentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public Filament? Filament { get; set; }
    public List<FilamentRequestStatusHistory> StatusHistory { get; set; } = new();
}
```

#### FilamentRequestStatusEnum
```csharp
public enum FilamentRequestStatusEnum
{
    Pending,
    Approved,
    Rejected
}
```

### Key Database Features

- **UUID v7 for Primary Keys**: All IDs use PostgreSQL's `uuidv7()` function for better database performance with time-ordered UUIDs
- **Enum Storage**: Status enums (`RequestStatusEnum`, `FilamentRequestStatusEnum`) are stored as strings in the database for readability
- **Unique Constraints**: Unique indexes on `User.DiscordId` and `User.GuestSessionToken`
- **Change Tracking**: Field-level change history via `PrintRequestChange` entity
- **Status History**: Complete audit trail of status changes via `StatusHistory` entity

## OpenAPI Specification

### Base URL
- Development: `https://localhost:7001`
- Production: `https://api.uberprints.com`

### Authentication
- JWT Bearer Token for authenticated endpoints
- Discord OAuth 2.0 for user authentication

### API Endpoints

#### Authentication Endpoints
- `GET /api/auth/login` - Initiate Discord OAuth login
- `GET /api/auth/discord/callback` - Discord OAuth callback handler
- `GET /api/auth/me` - Get current authenticated user (requires auth)
- `POST /api/auth/refresh` - Refresh JWT token (requires auth)
- `POST /api/auth/logout` - Logout user
- `POST /api/auth/guest` - Create guest session

#### Request Endpoints (Public)
- `GET /api/requests` - Get all public requests (or user's own private requests)
- `POST /api/requests` - Create new request (requires guest or authenticated session)
- `GET /api/requests/{id}` - Get specific request
- `GET /api/requests/track/{token}` - Track request by guest tracking token
- `PUT /api/requests/{id}` - Update own request (requires ownership)
- `DELETE /api/requests/{id}` - Delete own request (requires ownership)

#### Filament Endpoints (Public)
- `GET /api/filaments` - Get all filaments
- `GET /api/filaments/{id}` - Get filament by ID

#### Filament Request Endpoints (Public)
- `GET /api/filamentrequests` - Get all filament requests
- `POST /api/filamentrequests` - Create new filament request
- `GET /api/filamentrequests/{id}` - Get filament request by ID
- `PUT /api/filamentrequests/{id}` - Update own filament request
- `DELETE /api/filamentrequests/{id}` - Delete own filament request

#### Admin Endpoints (Require Admin Role)
- `GET /api/admin/requests` - Get all requests (including private)
- `PUT /api/admin/requests/{id}` - Update any request
- `PUT /api/admin/requests/{id}/status` - Change request status
- `POST /api/admin/filaments` - Create new filament
- `PUT /api/admin/filaments/{id}` - Update filament
- `PATCH /api/admin/filaments/{id}/stock` - Update filament stock
- `DELETE /api/admin/filaments/{id}` - Delete filament
- `GET /api/admin/filamentrequests` - Get all filament requests
- `PUT /api/admin/filamentrequests/{id}/status` - Change filament request status
- `POST /api/admin/filamentrequests/{id}/approve` - Approve and create filament

## Configuration


### Environment Variables

The application uses **DotNetEnv** to load configuration from a `.env` file for local development. Configuration is validated on startup using ASP.NET Core's Options pattern with data annotations.

#### Backend (.env)
```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=uberprints;Username=postgres;Password=password

# Discord OAuth (loaded into Discord section)
Discord__ClientId=your_discord_client_id
Discord__ClientSecret=your_discord_client_secret

# JWT Configuration (loaded into Jwt section, minimum 32 characters for SecretKey)
Jwt__SecretKey=your_jwt_secret_key_minimum_32_characters
Jwt__Issuer=UberPrints
Jwt__Audience=UberPrints
Jwt__ExpiryHours=168

# Frontend Configuration (for CORS)
Frontend__Url=http://localhost:5173

# Database Password (alternative to connection string)
POSTGRES_PASSWORD=password
```

#### Configuration Classes
- `DiscordOptions` - Discord OAuth settings (validated on startup)
- `JwtOptions` - JWT token settings with minimum 32-character secret requirement
- `FrontendOptions` - Frontend URL for CORS configuration

#### Frontend (.env)
```env
VITE_API_BASE_URL=https://localhost:7001
```

Note: Frontend uses Vite proxy in development to forward `/api` requests to the backend. In production, the backend serves the built frontend from `wwwroot`.

## Security Implementation

### Authentication Flow

#### Discord OAuth Flow
1. User clicks "Login with Discord"
2. Frontend redirects to `/api/auth/login` (optionally passing guest session token)
3. Backend initiates Discord OAuth challenge
4. Discord redirects to `/api/auth/discord/callback`
5. Backend exchanges code for access token
6. Backend retrieves user information from Discord (ID, username, global name, avatar)
7. Backend creates/updates user record in database
8. If guest session token provided, guest requests are linked to authenticated user
9. Backend generates JWT token with user claims (ID, username, IsAdmin, Role)
10. Backend creates authentication cookie (30-day expiration)
11. Backend redirects to frontend `/auth/callback?token={jwt}`
12. Frontend stores JWT token in localStorage
13. Frontend includes JWT in Authorization header and guest session token in `X-Guest-Session-Token` header

#### Guest Session Flow
1. User visits site without authentication
2. Frontend calls `/api/auth/guest` to create guest session
3. Backend creates guest user with `GuestSessionToken` and auto-generated username
4. Frontend stores guest session token
5. Frontend includes guest session token in `X-Guest-Session-Token` header for all requests
6. Guest can create, edit, and delete their own requests
7. When guest logs in via Discord, their requests are linked to their authenticated account

### Authorization
- **Dual Authentication**: Both JWT Bearer tokens and Cookie authentication supported
- JWT tokens contain user ID, username, IsAdmin flag, and optional Admin role
- Admin endpoints protected with `[Authorize(Roles = "Admin")]` attribute
- Ownership validation for user request modifications (checks user ID or guest session token)
- Private request visibility enforced at query level
- Guest session tokens validated via `X-Guest-Session-Token` header

### Security Features
- **HTTPS Only**: Cookie secure policy enforced in production
- **Forwarded Headers**: Support for reverse proxy (Cloudflare Tunnel)
- **CORS**: Configured for frontend URL from configuration
- **Session Management**: Distributed memory cache for session state
- **Input Validation**: Data annotations on models and DTOs
- **Configuration Validation**: Startup validation ensures all required config is present and valid

## Frontend Architecture

### Technology Stack
- **Framework**: React 18 with TypeScript 5.6
- **Build Tool**: Vite 5.4
- **Routing**: React Router v6
- **UI Components**: shadcn/ui (built on Radix UI primitives)
- **Styling**: Tailwind CSS 3.4
- **Form Handling**: React Hook Form 7.65 with Zod 4.1 validation
- **HTTP Client**: Axios 1.7
- **Icons**: Lucide React

### Key Frontend Features
- **Authentication Context**: Global auth state management (`AuthContext.tsx`)
- **Protected Routes**: Role-based route protection (`ProtectedRoute.tsx`)
- **Guest Session Management**: Automatic guest session creation and token storage
- **API Client**: Centralized API configuration with interceptors (`lib/api.ts`)
- **Type Safety**: Full TypeScript coverage with strict mode
- **Responsive Design**: Mobile-first approach with Tailwind CSS
- **Component Library**: Reusable shadcn/ui components in `components/ui/`

### Frontend Pages
- `Home.tsx` - Landing page
- `NewRequest.tsx` - Create print request form
- `EditRequest.tsx` - Edit existing request
- `RequestList.tsx` - View all public requests
- `RequestDetail.tsx` - View single request with history
- `TrackRequest.tsx` - Track request by token
- `Dashboard.tsx` - User dashboard with their requests
- `Profile.tsx` - User profile page
- `AdminDashboard.tsx` - Admin panel for managing requests
- `Filaments.tsx` - Filament catalog
- `FilamentRequests.tsx` - Filament request management
- `AuthCallback.tsx` - Discord OAuth callback handler

### Development vs Production
- **Development**: Vite dev server at `http://localhost:5173` with proxy to backend API
- **Production**: Static files built to `src/UberPrints.Server/wwwroot/` and served by ASP.NET Core
- Frontend build integrated into backend deployment

## Development Setup

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- PostgreSQL 18
- Docker & Docker Compose

### Local Development
```bash
# Start PostgreSQL
docker run --name uberprints-db -e POSTGRES_PASSWORD=password -e POSTGRES_DB=uberprints -p 5432:5432 -d postgres:18

# Backend setup
cd src/UberPrints.Server
dotnet restore
dotnet ef database update
dotnet run
# API available at https://localhost:7001
# Scalar API docs at https://localhost:7001/scalar

# Frontend setup (new terminal)
cd src/UberPrints.Client
npm install
npm run dev
# Frontend available at http://localhost:5173
# Uses Vite proxy to forward /api requests to backend
```

### Docker Development
```bash
# Start all services (database, server, cloudflared tunnel)
docker compose up -d --build

# Run database migrations for Docker deployment
./scripts/migrate-database.sh docker

# View logs
docker compose logs -f server

# Stop services
docker compose down
```

See [DEPLOYMENT.md](../DEPLOYMENT.md) for complete deployment guide with Cloudflare Tunnel setup.

## Deployment

### Production Dockerfile (Backend)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["backend/UberPrints.csproj", "backend/"]
RUN dotnet restore "backend/UberPrints.csproj"
COPY . .
WORKDIR "/src/backend"
RUN dotnet publish "UberPrints.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UberPrints.dll"]
```

### Production Dockerfile (Frontend)
```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production
COPY . .
RUN npm run build

FROM nginx:alpine AS production
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

## Monitoring and Logging

### Application Logging
- Structured logging with Serilog
- Log levels: Debug, Information, Warning, Error, Fatal
- Log sinks: Console, File, and external services
- Request/response logging for API endpoints

### Health Checks
```csharp
// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Performance Monitoring
- Response time tracking
- Database query performance
- Memory and CPU usage monitoring
- Error rate tracking

## Testing Strategy

### Backend Testing
- Unit tests for services and business logic
- Integration tests for API endpoints
- Database integration tests with test containers
- Authentication and authorization tests

### Frontend Testing
- Component unit tests with React Testing Library
- Integration tests for user workflows
- E2E tests with Playwright or Cypress
- Visual regression testing

### Test Coverage
- Minimum 80% code coverage for backend
- Minimum 70% code coverage for frontend
- Critical path coverage for user workflows

## CI/CD Pipeline

### Build Pipeline
1. Code checkout
2. Restore dependencies
3. Run linting and formatting checks
4. Run unit tests
5. Build application
6. Run integration tests
7. Build Docker images
8. Push to registry

### Deployment Pipeline
1. Deploy to staging environment
2. Run smoke tests
3. Deploy to production
4. Run health checks
5. Monitor for issues

## Scaling Considerations

### Database Scaling
- Read replicas for read-heavy operations
- Connection pooling optimization
- Query performance monitoring
- Database indexing strategy

### Application Scaling
- Horizontal scaling with load balancer
- Session state management (JWT is stateless)
- Caching strategy for frequently accessed data
- CDN for static assets

### Monitoring and Alerting
- Application performance monitoring
- Error tracking and alerting
- Resource utilization monitoring
- Custom metrics and dashboards
