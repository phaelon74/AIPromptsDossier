# Database Migration Guide

This guide explains how to set up the database for the AI Dungeon Prompts application.

## For New Installations (Recommended)

If you're setting up the application for the first time:

### Step 1: Run the Database Creation Script

```bash
psql -U postgres -f Setup/CreateDatabase.sql
```

This script will:
- Create the `aidungeonprompts` database
- Create the `aidungeonprompts_user` database user
- Create all required tables with proper relationships
- Set up indexes for performance
- Configure permissions

**That's it!** The script handles everything needed for a fresh installation.

---

## For Existing Installations (Adding Security Features)

If you already have an existing database and need to add the new security tables:

### Option 1: SQL Migration Script

Run this SQL script to add the new tables to your existing database:

```sql
-- Connect to your existing database
\c aidungeonprompts

-- Add new security tables
CREATE TABLE "LoginAttempts" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Success" BOOLEAN NOT NULL,
    "IpAddress" VARCHAR(45),
    "AttemptDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DateEdited" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "FK_LoginAttempts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_LoginAttempts_UserId" ON "LoginAttempts" ("UserId");
CREATE INDEX "IX_LoginAttempts_AttemptDate" ON "LoginAttempts" ("AttemptDate");

CREATE TABLE "AccountLockouts" (
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

CREATE INDEX "IX_AccountLockouts_UserId" ON "AccountLockouts" ("UserId");
CREATE INDEX "IX_AccountLockouts_UserId_IsActive" ON "AccountLockouts" ("UserId", "IsActive");

-- Grant permissions
GRANT ALL PRIVILEGES ON "LoginAttempts" TO aidungeonprompts_user;
GRANT ALL PRIVILEGES ON "AccountLockouts" TO aidungeonprompts_user;
GRANT USAGE, SELECT ON SEQUENCE "LoginAttempts_Id_seq" TO aidungeonprompts_user;
GRANT USAGE, SELECT ON SEQUENCE "AccountLockouts_Id_seq" TO aidungeonprompts_user;
```

Save this as `Setup/AddSecurityTables.sql` and run:

```bash
psql -U postgres -d aidungeonprompts -f Setup/AddSecurityTables.sql
```

### Option 2: Entity Framework Core Migrations (Future)

If you want to use EF Core migrations:

```bash
# From the project root directory
cd AIDungeonPrompts.Persistence

# Create a new migration
dotnet ef migrations add AddSecurityFeatures --startup-project ../AIDungeonPromptsWeb

# Apply the migration
dotnet ef database update --startup-project ../AIDungeonPromptsWeb
```

**Note:** The current codebase doesn't use EF migrations by default. If you want to switch to migrations, you'll need to:
1. Remove the manual SQL scripts
2. Generate an initial migration from the model snapshot
3. Apply migrations instead of running SQL scripts

---

## Verification

After running either approach, verify the tables exist:

```sql
\c aidungeonprompts
\dt

-- You should see:
-- LoginAttempts
-- AccountLockouts
-- (plus all existing tables)
```

Check table structure:

```sql
\d "LoginAttempts"
\d "AccountLockouts"
```

---

## Which Approach Should You Use?

### Use `CreateDatabase.sql` if:
- ✅ Fresh installation
- ✅ No existing data
- ✅ Simplest approach
- ✅ All tables created at once

### Use `AddSecurityTables.sql` if:
- ✅ Existing database with data
- ✅ Need to preserve existing data
- ✅ Upgrading from older version

### Use EF Migrations if:
- ✅ Want version-controlled schema changes
- ✅ Multiple developers needing consistent schema
- ✅ Complex deployment pipeline
- ⚠️ Requires additional setup and migration generation

---

## Troubleshooting

### "relation already exists" Error

If you get this error, the table already exists. Check:

```sql
SELECT tablename FROM pg_tables WHERE schemaname = 'public';
```

### Permission Denied

Ensure you're running as the postgres superuser or a user with CREATE privileges:

```bash
psql -U postgres -d aidungeonprompts
```

### Connection Refused

Ensure PostgreSQL is running:

```bash
sudo systemctl status postgresql
sudo systemctl start postgresql
```

---

## Recommendation

**For most users**: Use `CreateDatabase.sql` for new installations. It's the simplest and most reliable approach. The script is idempotent-safe and includes all necessary tables, indexes, and permissions.
