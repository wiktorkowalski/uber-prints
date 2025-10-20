import { request } from '@playwright/test';
import { execSync, spawn, ChildProcess } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import { GenericContainer, StartedTestContainer, Wait } from 'testcontainers';
import waitOn from 'wait-on';

/**
 * Global state to track started services
 */
interface GlobalTestState {
  postgresContainer?: StartedTestContainer;
  backendProcess?: ChildProcess;
  frontendProcess?: ChildProcess;
  connectionString?: string;
}

const state: GlobalTestState = {};

/**
 * Start PostgreSQL container using Testcontainers
 */
async function startPostgres(): Promise<StartedTestContainer> {
  console.log('  🐘 Starting PostgreSQL container...');

  const container = await new GenericContainer('postgres:18')
    .withEnvironment({
      POSTGRES_USER: 'postgres',
      POSTGRES_PASSWORD: 'testpassword',
      POSTGRES_DB: 'uberprints',
    })
    .withExposedPorts(5432)
    .withWaitStrategy(Wait.forLogMessage('database system is ready to accept connections', 2))
    .start();

  const host = container.getHost();
  const port = container.getMappedPort(5432);
  const connectionString = `Host=${host};Port=${port};Database=uberprints;Username=postgres;Password=testpassword`;

  console.log(`  ✓ PostgreSQL started on ${host}:${port}`);

  return container;
}

/**
 * Run EF Core migrations on the test database
 */
function runMigrations(connectionString: string): void {
  console.log('  🔄 Running database migrations...');

  const projectRoot = path.resolve(__dirname, '../..');
  const serverProject = path.join(projectRoot, 'src/UberPrints.Server/UberPrints.Server.csproj');

  try {
    // Update appsettings to use test connection string temporarily
    const appsettingsPath = path.join(
      projectRoot,
      'src/UberPrints.Server/appsettings.json'
    );
    const appsettings = JSON.parse(fs.readFileSync(appsettingsPath, 'utf-8'));
    const originalConnectionString = appsettings.ConnectionStrings.DefaultConnection;

    // Temporarily update connection string for migrations
    appsettings.ConnectionStrings.DefaultConnection = connectionString;
    fs.writeFileSync(appsettingsPath, JSON.stringify(appsettings, null, 2));

    try {
      execSync(`dotnet ef database update --project "${serverProject}"`, {
        cwd: projectRoot,
        stdio: 'pipe',
        env: {
          ...process.env,
          ConnectionStrings__DefaultConnection: connectionString,
        },
      });

      console.log('  ✓ Migrations applied successfully');
    } finally {
      // Restore original connection string
      appsettings.ConnectionStrings.DefaultConnection = originalConnectionString;
      fs.writeFileSync(appsettingsPath, JSON.stringify(appsettings, null, 2));
    }
  } catch (error) {
    console.error('  ❌ Migration failed:', (error as Error).message);
    throw error;
  }
}

/**
 * Seed the database with test data
 */
function seedDatabase(containerId: string): void {
  console.log('  📝 Seeding database with test data...');

  const seedFile = path.join(__dirname, 'seed-testdata.sql');

  if (!fs.existsSync(seedFile)) {
    console.warn('  ⚠️  Seed file not found, skipping seed');
    return;
  }

  try {
    // Read SQL file content
    const sqlContent = fs.readFileSync(seedFile, 'utf-8');

    // Execute SQL via docker exec (pipe SQL content to psql)
    execSync(`docker exec -i ${containerId} psql -U postgres -d uberprints`, {
      input: sqlContent,
      stdio: ['pipe', 'pipe', 'pipe'],
    });

    console.log('  ✓ Database seeded successfully');
  } catch (error) {
    console.error('  ⚠️  Seeding failed:', (error as Error).message);
    // Don't fail the setup if seeding fails - tests might still work
  }
}

/**
 * Start the ASP.NET Core backend server
 */
async function startBackend(connectionString: string): Promise<ChildProcess> {
  console.log('  🚀 Starting backend server...');

  const projectRoot = path.resolve(__dirname, '../..');
  const serverProject = path.join(projectRoot, 'src/UberPrints.Server/UberPrints.Server.csproj');

  // Start the backend process
  const backendProcess = spawn('dotnet', ['run', '--project', serverProject, '--no-build'], {
    cwd: projectRoot,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: 'http://localhost:5203',
      ConnectionStrings__DefaultConnection: connectionString,
      // Use test environment secrets if needed
      JWT_SECRET_KEY: process.env.JWT_SECRET_KEY || 'test-secret-key-minimum-32-characters-long-for-testing',
      DISCORD_CLIENT_ID: process.env.DISCORD_CLIENT_ID || 'test-discord-client-id',
      DISCORD_CLIENT_SECRET: process.env.DISCORD_CLIENT_SECRET || 'test-discord-client-secret',
    },
    stdio: 'pipe',
    detached: true, // Create new process group
  });

  // Log backend output for debugging
  backendProcess.stdout?.on('data', (data) => {
    const message = data.toString();
    if (message.includes('Now listening on')) {
      console.log(`  ✓ ${message.trim()}`);
    }
  });

  backendProcess.stderr?.on('data', (data) => {
    const message = data.toString();
    if (!message.includes('warn:')) {
      console.error(`  ⚠️  Backend: ${message.trim()}`);
    }
  });

  backendProcess.on('error', (error) => {
    console.error('  ❌ Backend process error:', error);
  });

  // Wait for backend to be ready
  console.log('  ⏳ Waiting for backend to be ready...');
  await waitOn({
    resources: ['http://localhost:5203/api/filaments'],
    timeout: 60000,
    interval: 1000,
    validateStatus: (status) => status === 200 || status === 401, // Accept 200 or 401 (unauthorized but running)
  });

  console.log('  ✓ Backend server is ready');

  return backendProcess;
}

/**
 * Start the Vite frontend dev server
 */
async function startFrontend(): Promise<ChildProcess> {
  console.log('  🎨 Starting frontend dev server...');

  const projectRoot = path.resolve(__dirname, '../..');
  const clientDir = path.join(projectRoot, 'src/UberPrints.Client');

  const frontendProcess = spawn('npm', ['run', 'dev'], {
    cwd: clientDir,
    env: {
      ...process.env,
    },
    stdio: 'pipe',
    shell: true,
    detached: true, // Create new process group
  });

  frontendProcess.stdout?.on('data', (data) => {
    const message = data.toString();
    if (message.includes('Local:') || message.includes('ready in')) {
      console.log(`  ✓ ${message.trim()}`);
    }
  });

  frontendProcess.stderr?.on('data', (data) => {
    const message = data.toString();
    // Vite outputs to stderr, so filter out normal messages
    if (!message.includes('VITE') && !message.includes('ready in')) {
      console.error(`  ⚠️  Frontend: ${message.trim()}`);
    }
  });

  // Wait for frontend to be ready
  console.log('  ⏳ Waiting for frontend to be ready...');
  await waitOn({
    resources: ['http://localhost:5173'],
    timeout: 120000,
    interval: 1000,
  });

  console.log('  ✓ Frontend dev server is ready');

  return frontendProcess;
}

/**
 * Verify the entire stack is working
 */
async function verifyStack(): Promise<void> {
  console.log('\n  🔍 Verifying test environment...\n');

  const apiContext = await request.newContext({
    baseURL: 'http://localhost:5203',
  });

  try {
    // Check backend API
    const filamentsResponse = await apiContext.get('/api/filaments');
    if (!filamentsResponse.ok()) {
      throw new Error(`API returned ${filamentsResponse.status()}`);
    }

    const filaments = await filamentsResponse.json();
    console.log(`  ✓ API accessible (${filaments.length} filaments)`);

    // Check frontend
    const frontendContext = await request.newContext({
      baseURL: 'http://localhost:5173',
    });

    const homeResponse = await frontendContext.get('/');
    if (!homeResponse.ok()) {
      throw new Error(`Frontend returned ${homeResponse.status()}`);
    }

    console.log('  ✓ Frontend accessible\n');

    await frontendContext.dispose();
  } catch (error) {
    console.error('\n  ❌ Stack verification failed:', (error as Error).message);
    throw error;
  } finally {
    await apiContext.dispose();
  }
}

/**
 * Global setup - runs once before all tests
 * Starts PostgreSQL container, runs migrations, seeds data, starts backend and frontend
 */
async function globalSetup() {
  console.log('\n🔧 Setting up complete test environment...\n');

  // Clean up any leftover processes from previous runs
  console.log('  🧹 Cleaning up any leftover processes...');
  killProcessOnPort(5203);
  killProcessOnPort(5173);
  await new Promise((resolve) => setTimeout(resolve, 2000)); // Wait 2 seconds for ports to be freed

  try {
    // Step 1: Start PostgreSQL
    state.postgresContainer = await startPostgres();
    const port = state.postgresContainer.getMappedPort(5432);
    state.connectionString = `Host=${state.postgresContainer.getHost()};Port=${port};Database=uberprints;Username=postgres;Password=testpassword`;

    // Step 2: Run migrations
    runMigrations(state.connectionString);

    // Step 3: Seed database
    seedDatabase(state.postgresContainer.getId());

    // Step 4: Start backend
    state.backendProcess = await startBackend(state.connectionString);

    // Step 5: Start frontend
    state.frontendProcess = await startFrontend();

    // Step 6: Verify everything is working
    await verifyStack();

    console.log('✅ Test environment ready!\n');

    // Store state for global teardown
    (global as any).__TEST_STATE__ = state;

    return async () => {
      // This function is called on teardown
      await globalTeardown();
    };
  } catch (error) {
    console.error('\n❌ Setup failed:', (error as Error).message);
    console.error('\nCleaning up...\n');
    await globalTeardown();
    throw error;
  }
}

/**
 * Kill a process and all its children
 */
function killProcessTree(pid: number, signal: string = 'SIGTERM'): void {
  try {
    // On Unix systems, kill the entire process group
    if (process.platform !== 'win32') {
      try {
        // Try to kill the process group
        process.kill(-pid, signal);
      } catch {
        // If that fails, try killing just the process
        process.kill(pid, signal);
      }
    } else {
      // On Windows, use taskkill
      execSync(`taskkill /pid ${pid} /T /F`, { stdio: 'ignore' });
    }
  } catch (error) {
    // Process might already be dead
  }
}

/**
 * Kill processes on specific ports (as backup)
 */
function killProcessOnPort(port: number): void {
  try {
    if (process.platform !== 'win32') {
      execSync(`lsof -ti:${port} | xargs kill -9 2>/dev/null || true`, {
        stdio: 'ignore',
      });
    } else {
      execSync(`FOR /F "tokens=5" %P IN ('netstat -a -n -o ^| findstr :${port}') DO TaskKill.exe /F /PID %P`, {
        stdio: 'ignore',
      });
    }
  } catch {
    // Ignore errors
  }
}

/**
 * Global teardown - runs once after all tests
 * Stops all services and containers
 */
async function globalTeardown() {
  console.log('\n🧹 Cleaning up test environment...\n');

  const teardownState = (global as any).__TEST_STATE__ || state;

  // Stop frontend
  if (teardownState.frontendProcess) {
    console.log('  🛑 Stopping frontend...');

    const frontendPid = teardownState.frontendProcess.pid;
    if (frontendPid) {
      killProcessTree(frontendPid, 'SIGTERM');
      await new Promise((resolve) => setTimeout(resolve, 1000));
      killProcessTree(frontendPid, 'SIGKILL');
    }

    // Backup: kill by port
    killProcessOnPort(5173);

    console.log('  ✓ Frontend stopped');
  }

  // Stop backend
  if (teardownState.backendProcess) {
    console.log('  🛑 Stopping backend...');

    const backendPid = teardownState.backendProcess.pid;
    if (backendPid) {
      killProcessTree(backendPid, 'SIGTERM');
      await new Promise((resolve) => setTimeout(resolve, 2000));
      killProcessTree(backendPid, 'SIGKILL');
    }

    // Backup: kill by port
    killProcessOnPort(5203);

    console.log('  ✓ Backend stopped');
  }

  // Stop PostgreSQL container
  if (teardownState.postgresContainer) {
    console.log('  🛑 Stopping PostgreSQL container...');
    await teardownState.postgresContainer.stop();
    console.log('  ✓ PostgreSQL stopped');
  }

  console.log('\n✅ Cleanup complete!\n');
}

export default globalSetup;
