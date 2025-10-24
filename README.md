# UberPrints

A 3D print request management system where users can submit print requests with optional delivery. Built for managing 3D printing services with features for request tracking, filament management, and admin oversight.

**Website**: [uber-prints.vicio.ovh](https://uber-prints.vicio.ovh)

## Features

- **Print Requests**: Submit 3D print requests with STL file upload, quantity, color preferences, and delivery options
- **Filament Catalog**: Browse available filament colors and materials
- **Request Tracking**: Track print status from submission to completion with anonymous tracking tokens
- **Guest Sessions**: Submit requests without registration, with optional account linking via Discord OAuth
- **Admin Dashboard**: Manage requests, update statuses, and maintain filament inventory
- **Status History**: Complete audit trail of request status changes

## Tech Stack

**Backend**
- ASP.NET Core 10.0 Web API
- PostgreSQL 18 database
- Entity Framework Core
- Discord OAuth authentication
- JWT + Cookie authentication

**Frontend**
- React + TypeScript
- Vite build tool
- Tailwind CSS + shadcn/ui components
- React Router
- Axios for API communication

**Testing**
- xUnit for unit and integration tests
- Testcontainers for isolated database testing
- Playwright for end-to-end browser testing

## Quick Start

### Prerequisites
- .NET 10.0 SDK
- Node.js 18+
- PostgreSQL 18 (or Docker)
- Discord OAuth application credentials

### Development Setup

1. Clone the repository
```bash
git clone https://github.com/wiktorkowalski/uber-prints.git
cd uber-prints
```

2. Configure environment variables
```bash
cp .env.example .env
# Edit .env with your Discord OAuth credentials and JWT secret
```

3. Start PostgreSQL (Docker)
```bash
docker run --name uberprints-db \
  -e POSTGRES_PASSWORD=password \
  -e POSTGRES_DB=uberprints \
  -p 5432:5432 \
  -d postgres:18
```

4. Run database migrations
```bash
dotnet ef database update --project src/UberPrints.Server
```

5. Start the backend
```bash
dotnet run --project src/UberPrints.Server/UberPrints.Server.csproj
# API available at https://localhost:7001
# Scalar API docs at https://localhost:7001/scalar
```

6. Start the frontend
```bash
cd src/UberPrints.Client
npm install
npm run dev
# Frontend available at http://localhost:5173
```

### Docker Deployment

```bash
docker compose up -d --build
```

See [DEPLOYMENT.md](./DEPLOYMENT.md) for complete deployment guide with Cloudflare Tunnel setup.

## Documentation

- [CLAUDE.md](./CLAUDE.md) - Complete development guide with build commands, architecture, and testing
- [DEPLOYMENT.md](./DEPLOYMENT.md) - Deployment instructions with Docker and Cloudflare Tunnel

## Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test test/UberPrints.Server.UnitTests

# Run integration tests only
dotnet test test/UberPrints.Server.IntegrationTests

# Run Playwright E2E tests
cd test/UberPrints.Client.Playwright
npm install
npx playwright install
npm test
```

## License

This project is for personal use.
