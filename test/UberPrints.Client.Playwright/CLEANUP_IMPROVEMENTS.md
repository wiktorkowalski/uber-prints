# Cleanup Improvements

## Problem Fixed

The initial implementation had issues with process cleanup - backend and frontend processes would remain running after tests completed, causing "port already in use" errors on subsequent test runs.

## Root Cause

When spawning processes with Node.js `spawn()`:
1. Child processes don't automatically die when parent is killed
2. Using `shell: true` creates additional intermediate shell processes
3. Simply calling `.kill()` on the process doesn't kill child processes
4. Process groups weren't being used, making it hard to kill entire process trees

## Solution

### 1. Process Group Management

Added `detached: true` to spawn options to create new process groups:

```typescript
const backendProcess = spawn('dotnet', ['run', '--project', serverProject], {
  // ... other options
  detached: true, // Creates new process group
});
```

### 2. Process Tree Killing

Implemented `killProcessTree()` function that kills entire process groups:

```typescript
function killProcessTree(pid: number, signal: string = 'SIGTERM'): void {
  try {
    if (process.platform !== 'win32') {
      // Kill the entire process group with negative PID
      process.kill(-pid, signal);
    } else {
      // Windows: use taskkill with /T flag
      execSync(`taskkill /pid ${pid} /T /F`);
    }
  } catch {
    // Process might already be dead
  }
}
```

### 3. Port-Based Backup Cleanup

Added `killProcessOnPort()` as a backup to kill any remaining processes:

```typescript
function killProcessOnPort(port: number): void {
  try {
    if (process.platform !== 'win32') {
      execSync(`lsof -ti:${port} | xargs kill -9 2>/dev/null || true`);
    } else {
      execSync(`FOR /F "tokens=5" %P IN ('netstat -a -n -o ^| findstr :${port}') DO TaskKill.exe /F /PID %P`);
    }
  } catch {
    // Ignore errors
  }
}
```

### 4. Pre-Startup Cleanup

Added cleanup before starting new processes to handle leftover processes from crashes:

```typescript
async function globalSetup() {
  console.log('  🧹 Cleaning up any leftover processes...');
  killProcessOnPort(5203); // Backend
  killProcessOnPort(5173); // Frontend
  await new Promise((resolve) => setTimeout(resolve, 1000));

  // ... rest of setup
}
```

### 5. Improved Teardown

Enhanced the teardown process to use both methods:

```typescript
async function globalTeardown() {
  // Method 1: Kill by process tree
  if (backendPid) {
    killProcessTree(backendPid, 'SIGTERM');
    await delay(2000);
    killProcessTree(backendPid, 'SIGKILL');
  }

  // Method 2: Kill by port (backup)
  killProcessOnPort(5203);
}
```

## Results

### Before
```bash
# First run
npm run test:full
# ✅ Success

# Second run
npm run test:full
# ❌ Error: Address already in use (port 5203)
# ❌ Error: Address already in use (port 5173)
```

### After
```bash
# First run
npm run test:full
# ✅ Success
# ✅ Cleanup complete (all processes killed)

# Second run
npm run test:full
# ✅ Pre-cleanup removes any leftovers
# ✅ Success
# ✅ Cleanup complete

# Third run (and so on...)
npm run test:full
# ✅ Always works!
```

## Verification

```bash
# After tests complete
lsof -ti:5203 -ti:5173 2>/dev/null | wc -l
# Output: 0 (no processes on these ports)

ps aux | grep -E "(dotnet run|node.*vite)" | grep -v grep | wc -l
# Output: 0 (no orphaned processes)
```

## Platform Support

The cleanup works on:
- ✅ macOS (tested)
- ✅ Linux (process.kill with negative PID)
- ✅ Windows (taskkill with /T flag)

## Edge Cases Handled

1. **Process already dead**: Wrapped in try-catch blocks
2. **Permission errors**: Silently ignored (process might be owned by another user)
3. **Port not in use**: Commands handle this gracefully
4. **Test interruption**: Pre-startup cleanup handles leftover processes
5. **Multiple runs**: Each run cleans up before and after

## Best Practices Implemented

1. **Graceful shutdown first**: Try SIGTERM before SIGKILL
2. **Wait between signals**: Give processes time to shut down cleanly
3. **Fallback mechanisms**: Use port-based killing as backup
4. **Cross-platform**: Different strategies for Unix/Windows
5. **Defensive coding**: Ignore errors where appropriate

## Monitoring

To verify cleanup is working, check logs for:
```
🧹 Cleaning up test environment...
  🛑 Stopping frontend...
  ✓ Frontend stopped
  🛑 Stopping backend...
  ✓ Backend stopped
  🛑 Stopping PostgreSQL container...
  ✓ PostgreSQL stopped
✅ Cleanup complete!
```

All steps should complete successfully without "address already in use" errors on the next run.
