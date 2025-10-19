-- Seed test data for Playwright E2E tests
-- This script adds filaments to the database for testing purposes

-- Insert test filaments if they don't exist
INSERT INTO "Filaments" ("Id", "Name", "Colour", "Material", "Brand", "StockAmount", "StockUnit", "CreatedAt", "UpdatedAt")
SELECT
    gen_random_uuid(),
    'PLA Black',
    '#000000',
    'PLA',
    'Test Brand',
    1000,
    'g',
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Filaments" WHERE "Name" = 'PLA Black'
);

INSERT INTO "Filaments" ("Id", "Name", "Colour", "Material", "Brand", "StockAmount", "StockUnit", "CreatedAt", "UpdatedAt")
SELECT
    gen_random_uuid(),
    'PLA White',
    '#FFFFFF',
    'PLA',
    'Test Brand',
    1000,
    'g',
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Filaments" WHERE "Name" = 'PLA White'
);

INSERT INTO "Filaments" ("Id", "Name", "Colour", "Material", "Brand", "StockAmount", "StockUnit", "CreatedAt", "UpdatedAt")
SELECT
    gen_random_uuid(),
    'PETG Blue',
    '#0000FF',
    'PETG',
    'Test Brand',
    800,
    'g',
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Filaments" WHERE "Name" = 'PETG Blue'
);

INSERT INTO "Filaments" ("Id", "Name", "Colour", "Material", "Brand", "StockAmount", "StockUnit", "CreatedAt", "UpdatedAt")
SELECT
    gen_random_uuid(),
    'ABS Red',
    '#FF0000',
    'ABS',
    'Test Brand',
    0,
    'g',
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Filaments" WHERE "Name" = 'ABS Red'
);

-- Verify the data
SELECT "Name", "Material", "Colour", "StockAmount"
FROM "Filaments"
ORDER BY "Name";
