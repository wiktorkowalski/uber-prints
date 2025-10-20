# Quick Start Guide

## TL;DR - Just Want to Run Tests?

### Absolute Fastest Way (Full Orchestration)

```bash
# Prerequisites: Docker running
docker ps

# Install & run
cd test/UberPrints.Client.Playwright
npm install
npx playwright install
npm run test:full
```

**Done!** PostgreSQL, backend, frontend - everything starts automatically.

---

## Two Ways to Run Tests

### Option 1: Full Orchestration ⭐ RECOMMENDED for CI

**What you need:**
- Docker running

**Commands:**
```bash
npm run test:full          # Everything automatic!
npm run test:full:ui       # With UI
npm run test:full:headed   # See browser
```

**What happens:**
1. ✅ PostgreSQL starts (Testcontainers)
2. ✅ Migrations run
3. ✅ Data seeds
4. ✅ Backend starts
5. ✅ Frontend starts
6. ✅ Tests run
7. ✅ Everything cleaned up

**Time:** ~40 seconds startup

---

### Option 2: Standard (Manual)

**What you need:**
- PostgreSQL running
- Backend running
- Frontend running (auto-started)

**Commands:**
```bash
# Terminal 1 - PostgreSQL
docker run --name uberprints-db \
  -e POSTGRES_PASSWORD=password \
  -e POSTGRES_DB=uberprints \
  -p 5432:5432 -d postgres:18

# Terminal 2 - Backend
dotnet run --project ../../src/UberPrints.Server/UberPrints.Server.csproj

# Terminal 3 - Tests
npm test
```

**Time:** ~10 seconds startup

---

## Common Commands

```bash
# Install
npm install
npx playwright install

# Run tests
npm test                    # Standard
npm run test:full          # Full orchestration
npm run test:ui            # Interactive mode
npm run test:headed        # See browser
npm run test:debug         # Debug mode

# View results
npm run report             # HTML report
```

---

## When to Use Which?

| Scenario | Use |
|----------|-----|
| CI/CD Pipeline | **Full Orchestration** |
| First time setup | **Full Orchestration** |
| Active development | Standard (faster) |
| Quick test iteration | Standard (faster) |
| Need clean environment | **Full Orchestration** |
| Debugging backend | Standard (see logs) |

---

## Troubleshooting

### "Docker not running"
```bash
# Start Docker
open -a Docker  # macOS
```

### "Port already in use"
```bash
# Kill processes
lsof -ti:5203 | xargs kill -9  # Backend
lsof -ti:5173 | xargs kill -9  # Frontend
```

### "Tests timing out"
- Check Docker has 4GB+ memory
- Ensure .NET SDK installed: `dotnet --version`
- Try `npm run test:full:headed` to see what's happening

---

## Quick Links

- **[Full Orchestration Guide](./FULL_ORCHESTRATION.md)** - Complete details
- **[Testing Guide](./TESTING_GUIDE.md)** - How to write tests
- **[README](./README.md)** - Full documentation

---

## Example: Running Your First Test

```bash
# 1. Check Docker is running
docker ps

# 2. Go to test directory
cd test/UberPrints.Client.Playwright

# 3. Install (first time only)
npm install
npx playwright install

# 4. Run ONE test with full orchestration
npm run test:full -- tests/home.spec.ts

# 5. View results
npm run report
```

**That's it!** 🎉

---

## What Tests Cover

✅ Home page & navigation
✅ Filaments catalog
✅ Create print requests
✅ View all requests
✅ Authentication & guest sessions
✅ Complete user workflows
✅ Mobile responsiveness
✅ Error handling
✅ Keyboard accessibility

42 tests across 5 browsers/devices!
