import { request } from '@playwright/test';
import { execSync } from 'child_process';
import * as fs from 'fs';

/**
 * Seed database using SQL via Docker
 */
function seedViaDocker(): boolean {
  try {
    // Path relative to project root
    const sqlFile = 'test/UberPrints.Client.Playwright/seed-testdata.sql';
    if (!fs.existsSync(sqlFile)) {
      console.warn('  ⚠️  Seed SQL file not found at', sqlFile);
      return false;
    }

    console.log('  📝 Executing SQL seed script via Docker...');
    execSync(
      `docker exec -i uberprints-db psql -U postgres -d uberprints < "${sqlFile}"`,
      { stdio: 'pipe' }
    );
    console.log('  ✓ Database seeded successfully via Docker');
    return true;
  } catch (error) {
    console.warn('  ⚠️  Could not seed via Docker:', (error as Error).message);
    return false;
  }
}

/**
 * Seed database using psql command
 */
function seedViaPsql(): boolean {
  try {
    // Path relative to project root
    const sqlFile = 'test/UberPrints.Client.Playwright/seed-testdata.sql';
    if (!fs.existsSync(sqlFile)) {
      console.warn('  ⚠️  Seed SQL file not found at', sqlFile);
      return false;
    }

    console.log('  📝 Executing SQL seed script via psql...');
    execSync(
      `PGPASSWORD=password psql -h localhost -U postgres -d uberprints -f "${sqlFile}"`,
      { stdio: 'pipe' }
    );
    console.log('  ✓ Database seeded successfully via psql');
    return true;
  } catch (error) {
    console.warn('  ⚠️  Could not seed via psql:', (error as Error).message);
    return false;
  }
}

/**
 * Global setup runs once before all tests
 * Automatically seeds the database with test data if needed
 */
async function globalSetup() {
  console.log('\n🔧 Setting up test environment...\n');

  const apiContext = await request.newContext({
    baseURL: 'http://localhost:5203',
  });

  try {
    // Check if API is accessible
    console.log('  🔍 Checking API connection...');
    const filamentsResponse = await apiContext.get('/api/filaments');

    if (!filamentsResponse.ok()) {
      console.error(`  ❌ API returned ${filamentsResponse.status()}`);
      console.error('  Make sure the backend server is running on http://localhost:5203');
      return;
    }

    const filaments = await filamentsResponse.json();
    console.log(`  ✓ API connected (found ${filaments.length} filaments)`);

    if (filaments.length === 0) {
      console.log('\n  📊 No filaments found - seeding database...\n');

      // Try Docker first (most common for this project)
      let seeded = seedViaDocker();

      // If Docker failed, try psql
      if (!seeded) {
        seeded = seedViaPsql();
      }

      if (!seeded) {
        console.error('\n  ❌ Could not automatically seed database!');
        console.error('  Please manually seed with one of these commands:');
        console.error('    docker exec -i uberprints-db psql -U postgres -d uberprints < test/UberPrints.Client.Playwright/seed-testdata.sql');
        console.error('    PGPASSWORD=password psql -h localhost -U postgres -d uberprints -f test/UberPrints.Client.Playwright/seed-testdata.sql');
        console.error('\n  Tests will likely fail without test data!\n');
      } else {
        // Verify seeding worked
        const verifyResponse = await apiContext.get('/api/filaments');
        const verifyFilaments = await verifyResponse.json();
        console.log(`\n  ✓ Verified: Database now has ${verifyFilaments.length} filaments\n`);
      }
    } else {
      console.log('  ✓ Database already has filaments - skipping seed\n');
    }
  } catch (error) {
    console.error('\n  ❌ Setup failed:', (error as Error).message);
    console.error('  Make sure:');
    console.error('    1. Backend server is running on http://localhost:5203');
    console.error('    2. PostgreSQL database is accessible');
    console.error('    3. Database has been migrated\n');
  } finally {
    await apiContext.dispose();
  }
}

export default globalSetup;
