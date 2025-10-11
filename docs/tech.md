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
- **Repository Pattern**: Data access abstraction
- **Service Layer**: Business logic encapsulation
- **Dependency Injection**: Loose coupling and testability
- **JWT Stateless Authentication**: Scalable authentication

## Database Schema

### Entity Models

#### User
```csharp
public class User
{
    public int Id { get; set; }
    public string? DiscordId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PrintRequest> PrintRequests { get; set; } = new();
}
```

#### PrintRequest
```csharp
public class PrintRequest
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? GuestTrackingToken { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string ModelUrl { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool RequestDelivery { get; set; }
    public int FilamentId { get; set; }
    public int CurrentStatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public Filament Filament { get; set; } = null!;
    public RequestStatus CurrentStatus { get; set; } = null!;
    public List<StatusHistory> StatusHistory { get; set; } = new();
}
```

#### RequestStatus
```csharp
public class RequestStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<PrintRequest> PrintRequests { get; set; } = new();
    public List<StatusHistory> StatusHistories { get; set; } = new();
}
```

#### StatusHistory
```csharp
public class StatusHistory
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int StatusId { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime Timestamp { get; set; }

    public PrintRequest Request { get; set; } = null!;
    public RequestStatus Status { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
```

#### Filament
```csharp
public class Filament
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public decimal StockAmount { get; set; }
    public string StockUnit { get; set; } = "grams";
    public string? Link { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<PrintRequest> PrintRequests { get; set; } = new();
}
```

### Initial Data Seeding

Initial data is seeded via EF Core migrations using the `OnModelCreating` method in `ApplicationDbContext`. This includes predefined request statuses and example filament data.

## OpenAPI Specification

### Base URL
- Development: `https://localhost:7001`
- Production: `https://api.uberprints.com`

### Authentication
- JWT Bearer Token for authenticated endpoints
- Discord OAuth 2.0 for user authentication

### API Endpoints

#### Authentication Endpoints
- `POST /api/auth/discord` - Discord OAuth callback
- `POST /api/auth/refresh` - Refresh JWT token
- `POST /api/auth/logout` - Logout user

#### Request Endpoints
- `GET /api/requests` - Get all requests with pagination and filtering
- `POST /api/requests` - Create new request (guest or authenticated)
- `GET /api/requests/{id}` - Get specific request
- `GET /api/requests/track/{token}` - Track guest request by token
- `PUT /api/requests/{id}` - Update own request (authenticated users)
- `DELETE /api/requests/{id}` - Delete own request (authenticated users)

#### Filament Endpoints
- `GET /api/filaments` - Get all filaments with optional stock filtering
- `GET /api/filaments/{id}` - Get filament by ID

#### Admin Endpoints
- `GET /api/admin/requests` - Get all requests (admin view)
- `PUT /api/admin/requests/{id}/status` - Change request status
- `POST /api/admin/filaments` - Create new filament
- `PUT /api/admin/filaments/{id}` - Update filament
- `PATCH /api/admin/filaments/{id}/stock` - Update filament stock
- `DELETE /api/admin/filaments/{id}` - Delete filament

## Configuration


### Environment Variables

#### Backend (.env)
```env
# Database
DATABASE_CONNECTION_STRING=Host=localhost;Database=uberprints;Username=postgres;Password=password

# Discord OAuth
DISCORD_CLIENT_ID=your_discord_client_id
DISCORD_CLIENT_SECRET=your_discord_client_secret
DISCORD_REDIRECT_URI=https://localhost:7001/api/auth/discord/callback

# JWT Configuration
JWT_SECRET_KEY=your_jwt_secret_key_here
JWT_ISSUER=UberPrints
JWT_AUDIENCE=UberPrints
JWT_EXPIRY_MINUTES=60

# Admin Configuration
ADMIN_DISCORD_ID=your_discord_user_id

# CORS
FRONTEND_URL=http://localhost:5173
```

#### Frontend (.env)
```env
VITE_API_BASE_URL=https://localhost:7001
VITE_DISCORD_CLIENT_ID=your_discord_client_id
VITE_DISCORD_REDIRECT_URI=http://localhost:5173/auth/callback
```

## Security Implementation

### Authentication Flow
1. User clicks "Login with Discord"
2. Redirect to Discord OAuth with proper scopes
3. Discord redirects to backend callback endpoint
4. Backend exchanges code for access token
5. Backend retrieves user information from Discord
6. Backend creates/updates user record
7. Backend generates JWT token
8. JWT token returned to frontend
9. Frontend stores token in localStorage
10. Frontend includes token in API requests

### Authorization
- JWT tokens contain user ID and admin status
- API endpoints validate JWT signature and expiration
- Role-based authorization for admin endpoints
- Ownership validation for user request modifications

### Security Headers
```csharp
// Security headers configuration
services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

services.AddAntiforgery();
```

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
cd backend
dotnet restore
dotnet ef database update
dotnet run

# Frontend setup (new terminal)
cd frontend
npm install
npm run dev
```

### Docker Development
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

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
