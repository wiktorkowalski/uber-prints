# UberPrints

A 3D print request management system where users can submit print requests with optional delivery. Built for managing 3D printing services with features for request tracking, filament management, and admin oversight.

**Website**: [uber-prints.vicio.ovh](https://uber-prints.vicio.ovh)

## Features

- **Print Requests**: Submit 3D print requests with STL file upload, quantity, color preferences, and delivery options
- **Filament Catalog**: Browse available filament colors and materials
- **Filament Requests**: Request new filaments to be added to inventory with admin approval workflow
- **Request Tracking**: Track print status from submission to completion with anonymous tracking tokens
- **Change Tracking**: Full audit trail of all modifications to print requests
- **Guest Sessions**: Submit requests without registration, with optional account linking via Discord OAuth
- **Admin Dashboard**: Manage requests, update statuses, and maintain filament inventory
- **Status History**: Complete audit trail of request status changes
- **Discord Notifications**: Automated DM notifications for admins on new requests and requesters on status changes
- **Thermal Printer**: Automatic receipt printing for new print requests with QR codes
- **Live Camera Streaming**: Watch the 3D printer in action via live RTSP → HLS streaming with DVR rewind buffer
- **Printer Monitoring**: Real-time printer status, temperatures, and print progress via Prusa Link integration

## Tech Stack

**Backend**
- ASP.NET Core 10.0 Web API
- PostgreSQL 18 database
- Entity Framework Core
- Discord OAuth authentication + Discord bot for notifications
- JWT + Cookie authentication
- FFmpeg for RTSP → HLS video streaming with DVR buffer
- Prusa Link API integration for printer monitoring
- External thermal printer API integration

**Frontend**
- React + TypeScript
- Vite build tool
- Tailwind CSS + shadcn/ui components
- TanStack Router for routing
- Axios for API communication
- HLS.js for video playback

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
- Discord bot token (for notifications)

### Development Setup

1. Clone the repository
```bash
git clone https://github.com/wiktorkowalski/uber-prints.git
cd uber-prints
```

2. Configure environment variables
```bash
cp .env.example .env
# Edit .env with your Discord OAuth credentials, Discord bot token, and JWT secret
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

## Camera Streaming Configuration

The application includes live camera streaming functionality for watching the 3D printer in action.

### How It Works
- **RTSP to HLS Conversion**: FFmpeg converts the RTSP camera stream to HLS format for browser compatibility
- **On-Demand Streaming**: Stream automatically starts when someone views the page and stops when no one is watching
- **Admin Controls**: Admins can temporarily enable/disable streaming (setting stored in memory, defaults to enabled)
- **Expected Latency**: 5-15 seconds (HLS protocol)

### Setup

1. **Configure Camera RTSP URL** in `appsettings.json`:
```json
{
  "Camera": {
    "RtspUrl": "rtsp://192.168.1.35/live",
    "HlsSegmentDuration": 2,
    "MaxSegments": 3,
    "OutputDirectory": "stream",
    "ConnectionTimeoutSeconds": 10
  }
}
```

2. **FFmpeg Installation**:
   - FFmpeg binaries are automatically downloaded on first run by the Xabe.FFmpeg library
   - Or install manually: `brew install ffmpeg` (macOS) or `apt install ffmpeg` (Linux)

3. **Access Live View**:
   - Navigate to `/live-view` on the website
   - Stream status and viewer count visible to all users
   - Admin controls available in the admin dashboard

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `RtspUrl` | RTSP URL of your camera | Required |
| `HlsSegmentDuration` | Duration of each HLS segment in seconds (lower = less latency) | 2 |
| `MaxSegments` | Number of HLS segments to keep (older segments auto-deleted) | 3 |
| `OutputDirectory` | Directory for HLS files (relative to wwwroot) | "stream" |
| `ConnectionTimeoutSeconds` | FFmpeg connection timeout | 10 |

### Troubleshooting

- **Stream won't start**: Check RTSP URL is accessible from the server
- **High latency**: Decrease `HlsSegmentDuration` (minimum 1 second)
- **Buffering issues**: Increase `MaxSegments` to keep more video buffered
- **FFmpeg errors**: Check logs in the application output for detailed error messages

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
