-- ===================================================
-- Add Security Tables to Existing Database
-- ===================================================
-- This script adds the new security tables to an existing
-- AI Dungeon Prompts database installation
-- ===================================================

-- Connect to your existing database
\c aidungeonprompts

-- ===================================================
-- Table: LoginAttempts
-- ===================================================
CREATE TABLE IF NOT EXISTS "LoginAttempts" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Success" BOOLEAN NOT NULL,
    "IpAddress" VARCHAR(45),
    "AttemptDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_LoginAttempts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_LoginAttempts_UserId" ON "LoginAttempts" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_LoginAttempts_AttemptDate" ON "LoginAttempts" ("AttemptDate");

-- ===================================================
-- Table: AccountLockouts
-- ===================================================
CREATE TABLE IF NOT EXISTS "AccountLockouts" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "LockoutStart" TIMESTAMP WITH TIME ZONE NOT NULL,
    "LockoutEnd" TIMESTAMP WITH TIME ZONE,
    "FailedAttempts" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "LockedByAdmin" TEXT,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_AccountLockouts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AccountLockouts_UserId" ON "AccountLockouts" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AccountLockouts_UserId_IsActive" ON "AccountLockouts" ("UserId", "IsActive");

-- ===================================================
-- Grant permissions to application user
-- ===================================================
GRANT ALL PRIVILEGES ON "LoginAttempts" TO aidungeonprompts_user;
GRANT ALL PRIVILEGES ON "AccountLockouts" TO aidungeonprompts_user;
GRANT USAGE, SELECT ON SEQUENCE "LoginAttempts_Id_seq" TO aidungeonprompts_user;
GRANT USAGE, SELECT ON SEQUENCE "AccountLockouts_Id_seq" TO aidungeonprompts_user;

-- ===================================================
-- Verification
-- ===================================================
\echo 'Security tables added successfully!'
\echo 'Verifying tables...'
SELECT 'LoginAttempts' as table_name, COUNT(*) as row_count FROM "LoginAttempts"
UNION ALL
SELECT 'AccountLockouts' as table_name, COUNT(*) as row_count FROM "AccountLockouts";

-- ===================================================
-- Complete!
-- ===================================================
