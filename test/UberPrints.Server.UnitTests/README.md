# UberPrints Server Unit Tests

This project contains unit tests for the UberPrints.Server API using xUnit and Moq for mocking dependencies.

## Test Structure

The tests are organized by controller:

- **RequestsControllerTests.cs** - Tests for public request endpoints
  - GET /api/requests
  - GET /api/requests/{id}
  - GET /api/requests/track/{token}
  - POST /api/requests
  - PUT /api/requests/{id}
  - DELETE /api/requests/{id}

- **FilamentsControllerTests.cs** - Tests for public filament endpoints
  - GET /api/filaments
  - GET /api/filaments?inStock=true
  - GET /api/filaments/{id}

- **AdminControllerTests.cs** - Tests for admin endpoints
  - GET /api/admin/requests
  - PUT /api/admin/requests/{id}/status
  - POST /api/admin/filaments
  - PUT /api/admin/filaments/{id}
  - PATCH /api/admin/filaments/{id}/stock
  - DELETE /api/admin/filaments/{id}
  - GET /api/admin/filaments/{id}

## Test Infrastructure

### TestBase
Base class providing:
- Mocked ApplicationDbContext
- Controller instances with mocked dependencies
- Helper methods for setting up DbSet mocks

### TestDataFactory
Factory class providing:
- Pre-configured test entities (Filament, PrintRequest, StatusHistory, User)
- Pre-configured DTOs for all operations
- Customizable test data with sensible defaults

## Test Coverage

Each test suite includes:
- ✅ **Happy path tests** - Verifying successful operations
- ✅ **Validation tests** - Testing error cases and constraints
- ✅ **Edge cases** - Out of stock filaments, invalid IDs, etc.
- ✅ **Mock verification** - Ensuring proper interaction with dependencies

## Running the Tests

### Run all tests:
```bash
cd test/UberPrints.Server.UnitTests
dotnet test
```

### Run tests with detailed output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run a specific test class:
```bash
dotnet test --filter "FullyQualifiedName~RequestsControllerTests"
```

### Run a specific test:
```bash
dotnet test --filter "FullyQualifiedName~RequestsControllerTests.CreateRequest_ReturnsCreatedRequest_WhenValidDataProvided"
```

## Mocking Strategy

- **ApplicationDbContext**: Fully mocked using Moq
- **DbSets**: Mocked with proper async method implementations
- **Entity Relationships**: Properly handled through includes and navigation properties
- **SaveChanges**: Mocked to return success (1) for all operations

## Notes

- Tests use in-memory mocking instead of actual database operations
- All external dependencies are mocked for fast, isolated testing
- Tests focus on controller logic and business rules
- Integration with actual database is covered by integration tests

## Troubleshooting

If tests fail due to mocking issues:
1. Ensure all required DbSets are properly mocked in TestBase.SetupMockDbSet()
2. Verify that entity relationships are correctly configured
3. Check that async methods are properly implemented in mocks
4. Ensure proper setup of FindAsync and other LINQ operations
