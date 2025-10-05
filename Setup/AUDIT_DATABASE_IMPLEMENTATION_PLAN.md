# Audit Database Implementation Plan

**Status:** Planned (Medium Priority - 3.5)  
**Complexity:** High  
**Estimated Effort:** 4-6 hours

---

## Overview

Currently, audit logs (`AuditPrompts` table) are stored in the same database as operational data. This presents a security risk: an attacker with database access could delete audit trails to cover their tracks.

**Recommendation:** Separate audit database on the same PostgreSQL server.

---

## Current Implementation

### Audit.EntityFramework Configuration

The application uses `Audit.EntityFramework` library:

```csharp
public class AIDungeonPromptsDbContext : AuditDbContext, IDataProtectionKeyContext, IAIDungeonPromptsDbContext
{
    public DbSet<AuditPrompt> AuditPrompts { get; set; }
    // ... other DbSets
}
```

**Issue:** `AuditDbContext` base class automatically handles audit logging to the same database.

---

## Proposed Solution

### Architecture

```
┌────────────────────────────────────────────────────────┐
│            PostgreSQL Server (127.0.0.1:5432)          │
│                                                        │
│  ┌──────────────────────┐      ┌──────────────────┐  │
│  │  aidungeonprompts    │      │  aidungeonprompts │  │
│  │  (Operational DB)    │      │  _audit           │  │
│  │                      │      │  (Audit-Only)     │  │
│  │  - Users             │      │  - AuditPrompts   │  │
│  │  - Prompts           │      │  - SystemChanges  │  │
│  │  - Tags              │      │  - UserActions    │  │
│  │  - ... (all tables)  │      │  (write-only)     │  │
│  └──────────────────────┘      └──────────────────┘  │
└────────────────────────────────────────────────────────┘
```

---

## Implementation Steps

### 1. Create Audit DbContext

**File:** `AIDungeonPrompts.Persistence/DbContexts/AIDungeonPromptsAuditDbContext.cs`

```csharp
using AIDungeonPrompts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIDungeonPrompts.Persistence.DbContexts
{
    public class AIDungeonPromptsAuditDbContext : DbContext
    {
        public AIDungeonPromptsAuditDbContext(DbContextOptions<AIDungeonPromptsAuditDbContext> options)
            : base(options)
        {
        }

        public DbSet<AuditPrompt> AuditPrompts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply only audit-related configurations
            modelBuilder.ApplyConfiguration(new AuditPromptConfiguration());
        }
    }
}
```

### 2. Update Main DbContext

**File:** `AIDungeonPrompts.Persistence/DbContexts/AIDungeonPromptsDbContext.cs`

```csharp
// REMOVE: public DbSet<AuditPrompt> AuditPrompts { get; set; }

// Keep inheriting from AuditDbContext but configure to use separate database
// via Audit.EntityFramework configuration in Startup.cs
```

### 3. Configure Audit.EntityFramework

**File:** `AIDungeonPromptsWeb/Startup.cs`

```csharp
// In ConfigureServices:
services.AddDbContext<AIDungeonPromptsAuditDbContext>(options =>
{
    options.UseNpgsql(GetAuditDatabaseConnectionString());
});

// Configure Audit.NET to use separate database
Audit.Core.Configuration.Setup()
    .UseEntityFramework(config => config
        .AuditTypeMapper(t => typeof(AuditPrompt))
        .AuditEntityAction<AuditPrompt>((ev, entry, entity) =>
        {
            // Write to audit database instead
            using var auditContext = new AIDungeonPromptsAuditDbContext(...);
            auditContext.AuditPrompts.Add(entity);
            auditContext.SaveChanges();
        }));
```

### 4. Create Audit Database Connection

**File:** `AIDungeonPromptsWeb/Startup.cs`

```csharp
private string GetAuditDatabaseConnectionString()
{
    // Use aidungeonprompts_audit database
    var connectionString = "Host=db;Port=5432;Database=aidungeonprompts_audit;Username=aidungeonprompts_audit_user;";
    
    // Read password from Docker Secret
    var secretPath = "/run/secrets/audit_db_password";
    if (File.Exists(secretPath))
    {
        var password = File.ReadAllText(secretPath).Trim();
        connectionString += $"Password={password};";
    }
    
    return connectionString;
}
```

### 5. Update CreateDatabase.sql

**File:** `Setup/CreateDatabase.sql`

Add audit database creation:

```sql
-- ===================================================
-- AUDIT DATABASE SETUP
-- ===================================================

-- Create audit database
CREATE DATABASE aidungeonprompts_audit
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

\c aidungeonprompts_audit;

-- Create audit user with limited permissions (INSERT only)
CREATE USER aidungeonprompts_audit_user WITH PASSWORD 'YOUR_AUDIT_PASSWORD';

-- Create AuditPrompts table
CREATE TABLE "AuditPrompts" (
    "Id" SERIAL PRIMARY KEY,
    "EntityType" TEXT NOT NULL,
    "EntityId" TEXT NOT NULL,
    "Action" TEXT NOT NULL,
    "Changes" JSONB,
    "Username" TEXT,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IPAddress" VARCHAR(45),
    "DateCreated" TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Grant INSERT-only permission to audit user
GRANT INSERT ON "AuditPrompts" TO aidungeonprompts_audit_user;
GRANT USAGE, SELECT ON SEQUENCE "AuditPrompts_Id_seq" TO aidungeonprompts_audit_user;

-- Prevent DELETE/UPDATE on audit logs
REVOKE DELETE, UPDATE ON "AuditPrompts" FROM aidungeonprompts_audit_user;

-- Create indexes for common queries (admin read access)
CREATE INDEX "IX_AuditPrompts_EntityType_EntityId" ON "AuditPrompts" ("EntityType", "EntityId");
CREATE INDEX "IX_AuditPrompts_Timestamp" ON "AuditPrompts" ("Timestamp");
CREATE INDEX "IX_AuditPrompts_Username" ON "AuditPrompts" ("Username");
```

### 6. Update Docker Secrets

**File:** `docker-compose.yml`

```yaml
secrets:
  db_password:
    file: ./secrets/db_password.txt
  serilog_db_password:
    file: ./secrets/serilog_db_password.txt
  audit_db_password:  # NEW
    file: ./secrets/audit_db_password.txt
```

**File:** `Setup/SetupDockerSecrets.md`

Add instructions for audit database secret:

```bash
echo -n "YOUR_AUDIT_DB_PASSWORD" > secrets/audit_db_password.txt
chmod 600 secrets/audit_db_password.txt
```

### 7. Create Migration for Existing Systems

**File:** `Setup/MigrateToSeparateAuditDb.sql`

```sql
-- Export existing audit data
\c aidungeonprompts
\copy (SELECT * FROM "AuditPrompts") TO '/tmp/audit_export.csv' WITH CSV HEADER;

-- Create audit database (if following CreateDatabase.sql)
-- Then import data
\c aidungeonprompts_audit
\copy "AuditPrompts" FROM '/tmp/audit_export.csv' WITH CSV HEADER;

-- Cleanup
\c aidungeonprompts
DROP TABLE IF EXISTS "AuditPrompts";
```

---

## Security Benefits

### Before (Current)
❌ Audit logs in same database  
❌ Application user can delete audit logs  
❌ Compromised database = compromised audit trail  

### After (Proposed)
✅ Audit logs in separate database  
✅ Audit user has INSERT-only permission  
✅ Cannot delete or modify audit logs  
✅ Separate credentials for audit database  
✅ Even if operational DB is compromised, audit trail remains intact  

---

## Testing Checklist

- [ ] Create audit database and user
- [ ] Configure audit database connection
- [ ] Verify audit logs written to separate database
- [ ] Confirm operational database no longer has AuditPrompts
- [ ] Test INSERT permission works
- [ ] Test DELETE/UPDATE are denied for audit user
- [ ] Verify admin can read audit logs
- [ ] Test with Docker Secrets
- [ ] Migrate existing audit data
- [ ] Document admin access pattern for audit review

---

## Admin Access to Audit Logs

Since audit user has INSERT-only, admins need separate read access:

```csharp
// In AdminController
[Authorize(Policy = PolicyValueConstants.AdminsOnly)]
public async Task<IActionResult> AuditLogs(string entityType, int? entityId, CancellationToken cancellationToken)
{
    // Admin uses main database credentials to read audit logs
    using var auditContext = new AIDungeonPromptsAuditDbContext(...);
    
    var logs = await auditContext.AuditPrompts
        .Where(a => entityType == null || a.EntityType == entityType)
        .Where(a => entityId == null || a.EntityId == entityId.ToString())
        .OrderByDescending(a => a.Timestamp)
        .Take(100)
        .ToListAsync(cancellationToken);
    
    return View(logs);
}
```

---

## Alternative: Write-Only Logging Service

For even better security, consider:

1. **Syslog Integration:** Send audit logs to external syslog server
2. **AWS CloudWatch / Azure Monitor:** Cloud-based audit logging
3. **Separate Audit Microservice:** Dedicated service with own database

**Pros:**
- Complete isolation from application
- Tamper-proof audit trail
- Centralized logging

**Cons:**
- More complex infrastructure
- Additional dependencies
- Requires network connectivity

---

## Rollback Plan

If issues arise:

1. **Stop Application:** `docker-compose down`
2. **Restore Single Database:** Merge audit DB back into main
3. **Revert Code Changes:** Use git to revert DbContext changes
4. **Restart:** `docker-compose up -d`

---

## Estimated Timeline

| Task | Time | Priority |
|------|------|----------|
| Create audit DbContext | 1h | High |
| Configure Audit.EntityFramework | 2h | High |
| Update SQL scripts | 1h | High |
| Testing | 1h | High |
| Documentation | 1h | Medium |
| **Total** | **6h** | |

---

## Decision

**Recommendation:** Implement separate audit database  
**Timing:** Can be done in Phase 2 after current security fixes are deployed  
**Priority:** Medium (enhances security but not critical vulnerability)

**Why defer?**
- Complex architectural change
- Requires thorough testing
- Current system is functional
- Can be implemented without downtime if planned properly

---

## References

- [Audit.NET Documentation](https://github.com/thepirat000/Audit.NET)
- [Entity Framework Audit Provider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/README.md)
- [PostgreSQL User Permissions](https://www.postgresql.org/docs/current/sql-grant.html)

---

**Last Updated:** October 2025  
**Status:** Planning Phase
