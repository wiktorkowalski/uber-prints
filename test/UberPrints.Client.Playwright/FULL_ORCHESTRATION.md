# Full Test Orchestration with Testcontainers

This directory contains two Playwright configurations:

## Configuration Options

### 1. Standard Config (playwright.config.ts) - Default

**What it does:**
- ✅ Starts frontend automatically (Vite dev server)
- ❌ Requires you to start backend manually
- ❌ Requires you to start PostgreSQL manually

**Use when:**
- You're actively developing and already have backend/database running
- You want faster test startup (no container overhead)
- You're debugging and need to inspect the backend logs

**Run with:**
```bash
npm test
```

**Prerequisites:**
```bash
# Terminal 1 - Start PostgreSQL (Docker or local)
docker run --name uberprints-db \
  -e POSTGRES_PASSWORD=password \
  -e POSTGRES_DB=uberprints \
  -p 5432:5432 \
  -d postgres:18

# Terminal 2 - Run migrations
dotnet ef database update --project ../../src/UberPrints.Server

# Terminal 3 - Start backend
dotnet run --project ../../src/UberPrints.Server/UberPrints.Server.csproj

# Terminal 4 - Run tests
npm test
```

### 2. Full Orchestration Config (playwright.config.full.ts) - Recommended for CI

**What it does:**
- ✅ Starts PostgreSQL automatically (Testcontainers)
- ✅ Runs database migrations automatically
- ✅ Seeds test data automatically
- ✅ Starts backend automatically
- ✅ Starts frontend automatically
- ✅ Cleans up everything when done

**Use when:**
- Running tests in CI/CD
- You want a completely isolated test environment
- You don't have anything running locally
- You want zero manual setup

**Run with:**
```bash
npm run test:full
```

**Prerequisites:**
- Docker must be running (for Testcontainers)
- Nothing else needed!

## Detailed Setup: Full Orchestration

### Installation

1. **Install dependencies:**
   ```bash
   cd test/UberPrints.Client.Playwright
   npm install
   npx playwright install
   ```

2. **Ensure Docker is running:**
   ```bash
   docker ps
   ```
   If this fails, start Docker Desktop or your Docker daemon.

### Running Full Orchestration Tests

#### Basic Usage

```bash
# Run all tests with full orchestration
npm run test:full

# Run with UI mode
npm run test:full:ui

# Run in headed mode (see browser)
npm run test:full:headed

# Run specific test file
npx playwright test --config=playwright.config.full.ts tests/home.spec.ts

# Run specific browser
npx playwright test --config=playwright.config.full.ts --project=chromium
```

#### What Happens During Test Run

```
🔧 Setting up complete test environment...

  🐘 Starting PostgreSQL container...
  ✓ PostgreSQL started on port 54321
  ✓ Connection: Host=localhost;Port=54321;Database=uberprints;Username=postgres;Password=testpassword

  🔄 Running database migrations...
  ✓ Migrations applied successfully

  📝 Seeding database with test data...
  ✓ Database seeded successfully

  🚀 Starting backend server...
  ⏳ Waiting for backend to be ready...
  ✓ Now listening on: http://localhost:5203
  ✓ Backend server is ready

  🎨 Starting frontend dev server...
  ⏳ Waiting for frontend to be ready...
  ✓ VITE v5.x.x ready in 1234 ms
  ✓ Frontend dev server is ready

  🔍 Verifying test environment...

  ✓ API accessible (5 filaments)
  ✓ Frontend accessible

✅ Test environment ready!

Running 42 tests using 4 workers...

[Tests run here...]

🧹 Cleaning up test environment...

  🛑 Stopping frontend...
  ✓ Frontend stopped

  🛑 Stopping backend...
  ✓ Backend stopped

  🛑 Stopping PostgreSQL container...
  ✓ PostgreSQL stopped

✅ Cleanup complete!
```

## How It Works

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│ Playwright Test Runner                                  │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │ Global Setup (global-setup-full.ts)            │    │
│  │                                                  │    │
│  │  1. Start PostgreSQL (Testcontainers)           │    │
│  │     └─> postgres:18 container                   │    │
│  │                                                  │    │
│  │  2. Run EF Core Migrations                      │    │
│  │     └─> dotnet ef database update               │    │
│  │                                                  │    │
│  │  3. Seed Test Data                              │    │
│  │     └─> Execute seed-testdata.sql               │    │
│  │                                                  │    │
│  │  4. Start Backend                               │    │
│  │     └─> dotnet run (http://localhost:5203)      │    │
│  │                                                  │    │
│  │  5. Start Frontend                              │    │
│  │     └─> npm run dev (http://localhost:5173)     │    │
│  │                                                  │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │ Run All Tests                                   │    │
│  │  - home.spec.ts                                 │    │
│  │  - new-request.spec.ts                          │    │
│  │  - requests.spec.ts                             │    │
│  │  - filaments.spec.ts                            │    │
│  │  - auth.spec.ts                                 │    │
│  │  - e2e-workflow.spec.ts                         │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │ Global Teardown                                 │    │
│  │  1. Stop Frontend                               │    │
│  │  2. Stop Backend                                │    │
│  │  3. Stop PostgreSQL Container                   │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### Components

#### 1. Testcontainers (PostgreSQL)

The setup uses [Testcontainers](https://testcontainers.com/) to spin up a real PostgreSQL 18 container:

```typescript
const container = await new PostgreSqlContainer('postgres:18')
  .withDatabase('uberprints')
  .withUsername('postgres')
  .withPassword('testpassword')
  .withExposedPorts(5432)
  .start();
```

**Benefits:**
- Real PostgreSQL instance (not mocked)
- Isolated test data
- Automatic cleanup
- Same version as production

#### 2. EF Core Migrations

Applies all migrations to the test database:

```typescript
execSync(`dotnet ef database update --project "${serverProject}"`, {
  env: {
    ConnectionStrings__DefaultConnection: connectionString,
  },
});
```

#### 3. Data Seeding

Executes `seed-testdata.sql` to populate test data:

```sql
-- Filaments, users, sample requests, etc.
```

#### 4. Backend Server

Spawns the ASP.NET Core server as a child process:

```typescript
const backendProcess = spawn('dotnet', ['run', '--project', serverProject], {
  env: {
    ASPNETCORE_URLS: 'http://localhost:5203',
    ConnectionStrings__DefaultConnection: connectionString,
  },
});
```

#### 5. Frontend Server

Spawns the Vite dev server:

```typescript
const frontendProcess = spawn('npm', ['run', 'dev'], {
  cwd: clientDir,
});
```

#### 6. Wait-on Utilities

Uses `wait-on` to ensure services are ready before tests start:

```typescript
await waitOn({
  resources: ['http://localhost:5203/api/filaments'],
  timeout: 60000,
});
```

## Environment Variables

The full orchestration setup uses these environment variables:

### Required for Backend
- `JWT_SECRET_KEY` - Defaults to test key if not set
- `DISCORD_CLIENT_ID` - Defaults to test ID if not set
- `DISCORD_CLIENT_SECRET` - Defaults to test secret if not set

### Automatically Set
- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://localhost:5203`
- `ConnectionStrings__DefaultConnection` - From Testcontainer

## Troubleshooting

### Docker Not Running

**Error:**
```
Error: Cannot connect to Docker daemon
```

**Solution:**
```bash
# Start Docker Desktop or Docker daemon
open -a Docker  # macOS
# or
sudo systemctl start docker  # Linux
```

### Port Already in Use

**Error:**
```
Address already in use: http://localhost:5203
```

**Solution:**
```bash
# Find and kill the process using the port
lsof -ti:5203 | xargs kill -9
lsof -ti:5173 | xargs kill -9
```

### Migrations Fail

**Error:**
```
Migration failed: Database connection error
```

**Solution:**
- Ensure Docker has enough memory (4GB+ recommended)
- Check Docker container logs
- Verify .NET SDK is installed: `dotnet --version`

### Testcontainers Timeout

**Error:**
```
Container did not start within timeout
```

**Solution:**
```bash
# Pull the PostgreSQL image manually first
docker pull postgres:18

# Increase Docker resources in Docker Desktop settings
```

### Backend Won't Start

**Error:**
```
Backend not ready after 60 seconds
```

**Solution:**
```bash
# Check if backend can be built
cd ../../src/UberPrints.Server
dotnet build

# Check for port conflicts
lsof -ti:5203
```

### Frontend Won't Start

**Error:**
```
Frontend not ready after 120 seconds
```

**Solution:**
```bash
# Ensure npm dependencies are installed
cd ../../src/UberPrints.Client
npm install

# Try starting manually to see errors
npm run dev
```

## Performance Considerations

### Startup Time

**Full orchestration:**
- PostgreSQL container: ~5-10 seconds
- Migrations: ~5 seconds
- Seeding: ~1 second
- Backend: ~10-15 seconds
- Frontend: ~5-10 seconds
- **Total:** ~30-40 seconds

**Standard config (manual):**
- Frontend only: ~5-10 seconds
- **Total:** ~5-10 seconds

### When to Use Each

| Scenario | Configuration | Reason |
|----------|---------------|--------|
| CI/CD Pipeline | Full | Complete isolation, reproducible |
| First-time setup | Full | Zero manual steps |
| Active development | Standard | Faster, easier debugging |
| Testing specific feature | Standard | Quick iterations |
| Production-like testing | Full | Real database, full stack |
| Debugging backend | Standard | Direct access to logs |

## CI/CD Integration

### GitHub Actions Example

```yaml
name: E2E Tests

on: [push, pull_request]

jobs:
  playwright-tests:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '20'

      - name: Install Playwright dependencies
        working-directory: test/UberPrints.Client.Playwright
        run: |
          npm ci
          npx playwright install --with-deps

      - name: Run E2E tests with full orchestration
        working-directory: test/UberPrints.Client.Playwright
        run: npm run test:full
        env:
          JWT_SECRET_KEY: ${{ secrets.JWT_SECRET_KEY }}
          DISCORD_CLIENT_ID: ${{ secrets.DISCORD_CLIENT_ID }}
          DISCORD_CLIENT_SECRET: ${{ secrets.DISCORD_CLIENT_SECRET }}

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-report
          path: test/UberPrints.Client.Playwright/playwright-report/
```

## Comparison: Standard vs Full

| Feature | Standard Config | Full Config |
|---------|----------------|-------------|
| PostgreSQL | Manual | ✅ Testcontainers |
| Migrations | Manual | ✅ Automatic |
| Data Seeding | Manual | ✅ Automatic |
| Backend | Manual | ✅ Automatic |
| Frontend | ✅ Automatic | ✅ Automatic |
| Setup Time | Fast (~10s) | Slower (~40s) |
| Isolation | Low | High |
| CI-Friendly | ❌ | ✅ |
| Debug-Friendly | ✅ | ❌ |
| Zero Dependencies | ❌ | ✅ (just Docker) |

## Best Practices

1. **Use full config in CI:** Ensures consistent, isolated test environment
2. **Use standard config locally:** Faster iteration during development
3. **Keep test data minimal:** Faster seeding, easier debugging
4. **Monitor resource usage:** Docker containers need adequate memory
5. **Clean up orphaned containers:** `docker ps -a | grep postgres`

## Advanced Usage

### Custom Database Seeding

Edit `seed-testdata.sql` to add custom test data:

```sql
-- Add more filaments
INSERT INTO "Filaments" (...) VALUES (...);

-- Add test users
INSERT INTO "Users" (...) VALUES (...);

-- Add sample requests
INSERT INTO "PrintRequests" (...) VALUES (...);
```

### Custom Environment Variables

Create a `.env.test` file:

```bash
JWT_SECRET_KEY=my-test-key-minimum-32-characters
DISCORD_CLIENT_ID=test-client-id
DISCORD_CLIENT_SECRET=test-secret
```

Load in global setup:

```typescript
import * as dotenv from 'dotenv';
dotenv.config({ path: '.env.test' });
```

### Debugging Setup Process

Add verbose logging:

```typescript
// In global-setup-full.ts
backendProcess.stdout?.on('data', (data) => {
  console.log(`Backend: ${data.toString()}`); // Log everything
});
```

### Using Different PostgreSQL Version

```typescript
const container = await new PostgreSqlContainer('postgres:16') // Change version
  .withDatabase('uberprints')
  // ... rest of config
```

## Resources

- [Testcontainers Documentation](https://testcontainers.com/)
- [Playwright Global Setup](https://playwright.dev/docs/test-global-setup-teardown)
- [wait-on Documentation](https://github.com/jeffbski/wait-on)
- [UberPrints Testing Guide](./TESTING_GUIDE.md)
