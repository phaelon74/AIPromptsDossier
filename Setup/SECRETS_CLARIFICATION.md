# User Secrets vs Docker Secrets - Clarification Guide

**Version:** 2.1.0  
**Date:** October 2025

---

## Purpose of This Document

This document exists because there's often confusion between **User Secrets** and **Docker Secrets**. They sound similar but are completely different technologies for different scenarios.

---

## TL;DR (Too Long; Didn't Read)

**Choose based on HOW you run the application:**

| How You Run It | What To Use | Setup Guide |
|----------------|-------------|-------------|
| `docker-compose up` | **Docker Secrets** | `Setup/SetupDockerSecrets.md` |
| `dotnet run` | **User Secrets** | `Setup/UserSecretsSetup.md` |

**90% of users should use Docker Secrets** (production deployments).  
**User Secrets are only for developers doing local development**.

---

## Detailed Comparison

### Docker Secrets 🐳

**What It Is:**
- Docker's built-in secret management system
- Files mounted into containers at `/run/secrets/`
- Part of Docker Compose and Docker Swarm

**When To Use:**
- ✅ Production deployments
- ✅ Running with `docker-compose up`
- ✅ Any Docker-based deployment
- ✅ **This is the default for this project**

**Where Secrets Are Stored:**
```
/path/to/project/secrets/
├── db_password.txt
└── serilog_db_password.txt
```

**How They're Used:**
```yaml
# docker-compose.yml
secrets:
  db_password:
    file: ./secrets/db_password.txt  # Read from file
```

**Setup:**
```bash
# 1. Create secrets directory
mkdir -p secrets

# 2. Create password files
echo -n "YOUR_SECURE_PASSWORD" > secrets/db_password.txt
echo -n "YOUR_SECURE_PASSWORD" > secrets/serilog_db_password.txt

# 3. Secure them
chmod 600 secrets/*.txt

# 4. Add to .gitignore
echo "secrets/" >> .gitignore

# 5. Done! Docker Compose will mount them automatically
docker-compose up -d
```

**Documentation:** `Setup/SetupDockerSecrets.md`

---

### User Secrets 💻

**What It Is:**
- ASP.NET Core's development secrets system
- JSON file stored in your user profile
- **Only works in Development environment**

**When To Use:**
- ✅ Local development with `dotnet run`
- ✅ Debugging in Visual Studio/VS Code
- ✅ Development WITHOUT Docker
- ❌ **NOT for production**
- ❌ **NOT used when running in Docker**

**Where Secrets Are Stored:**
```
# Linux/Mac
~/.microsoft/usersecrets/<user-secrets-id>/secrets.json

# Windows
%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json
```

**How They're Used:**
```bash
# Command line
dotnet user-secrets set "ConnectionStrings:AIDungeonPrompt" "Host=localhost;...;Password=DEV_PASSWORD;"

# Or in Visual Studio: Right-click project → Manage User Secrets
```

**Setup:**
```bash
# 1. Navigate to project
cd AIDungeonPromptsWeb

# 2. Initialize (creates UserSecretsId in .csproj)
dotnet user-secrets init

# 3. Set your connection string with password
dotnet user-secrets set "ConnectionStrings:AIDungeonPrompt" "Host=localhost;Port=5432;Database=aidungeonprompts;Username=aidungeonprompts_user;Password=YOUR_DEV_PASSWORD;"

# 4. Run application
dotnet run

# User Secrets automatically loaded in Development environment
```

**Documentation:** `Setup/UserSecretsSetup.md`

---

## Side-by-Side Comparison

| Feature | User Secrets | Docker Secrets |
|---------|--------------|----------------|
| **Technology** | ASP.NET Core | Docker / Docker Compose |
| **Environment** | Development only | Production (any environment) |
| **Storage Location** | User profile directory | Project `secrets/` directory |
| **Format** | JSON file | Plain text files |
| **Access In Container** | Not available | Mounted at `/run/secrets/` |
| **Git Commit** | Never committed (outside repo) | Never committed (.gitignore) |
| **Requires Docker** | No | Yes |
| **Used By** | `dotnet run` | `docker-compose up` |
| **Setup Complexity** | Simple | Simple |
| **Use Case** | Developer machines | Production servers |

---

## Common Scenarios

### Scenario 1: Production Deployment (Most Common)

**What you're doing:**
- Deploying to a server
- Running with Docker Compose
- Production environment

**What to use:**
```bash
# Docker Secrets
mkdir -p secrets
echo -n "PROD_PASSWORD" > secrets/db_password.txt
echo -n "PROD_PASSWORD" > secrets/serilog_db_password.txt
chmod 600 secrets/*.txt
docker-compose up -d --build
```

**Don't use:** User Secrets (they won't work in Docker)

---

### Scenario 2: Local Development Without Docker

**What you're doing:**
- Developing on your laptop
- Running `dotnet run` or debugging in IDE
- Testing code changes quickly

**What to use:**
```bash
# User Secrets
cd AIDungeonPromptsWeb
dotnet user-secrets set "ConnectionStrings:AIDungeonPrompt" "Host=localhost;...;Password=DEV_PASSWORD;"
dotnet run
```

**Don't use:** Docker Secrets (you're not running Docker)

---

### Scenario 3: Developer Using Docker Locally

**What you're doing:**
- Developing on your laptop
- Running with Docker to match production
- Testing containerized deployment

**What to use:**
```bash
# Docker Secrets
mkdir -p secrets
echo -n "DEV_PASSWORD" > secrets/db_password.txt
echo -n "DEV_PASSWORD" > secrets/serilog_db_password.txt
chmod 600 secrets/*.txt
docker-compose up -d --build
```

**Don't use:** User Secrets (Docker ignores them)

---

### Scenario 4: Team with Mixed Workflows

**What you're doing:**
- Some developers use `dotnet run`
- Others use Docker
- Everyone needs to work

**What to use:**
```bash
# Setup BOTH (they don't conflict)

# For dotnet run developers:
cd AIDungeonPromptsWeb
dotnet user-secrets set "ConnectionStrings:AIDungeonPrompt" "..."

# For Docker developers:
mkdir -p secrets
echo -n "PASSWORD" > secrets/db_password.txt
```

**Result:** Everyone can work in their preferred way

---

## How The Application Chooses

The application automatically uses the right secret based on how it's running:

### When Running with `dotnet run`:
1. Loads `appsettings.json` → Gets connection string WITHOUT password
2. Loads `appsettings.Development.json` → Still no password
3. Loads **User Secrets** → Gets password from User Secrets
4. Final connection string: From appsettings + password from User Secrets ✅

### When Running with Docker:
1. Loads `appsettings.json` → Gets connection string WITHOUT password
2. Environment variable from docker-compose.yml overrides it
3. Reads `/run/secrets/db_password` → Gets password from Docker Secret
4. Appends password to connection string
5. Final connection string: From environment + password from Docker Secret ✅

**The code in `Startup.cs` handles this automatically.**

---

## Code Reference

**In `Startup.cs`:**

```csharp
private string GetDatabaseConnectionString()
{
    // Get base connection string (from appsettings or environment)
    var connectionString = Configuration.GetConnectionString(DatabaseConnectionName);
    
    // Validate it exists
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Database connection string not configured");
    }
    
    // If running in Docker, read password from Docker Secret
    if (File.Exists(ConfigurationConstants.DatabasePasswordSecretPath))  // /run/secrets/db_password
    {
        var password = File.ReadAllText(ConfigurationConstants.DatabasePasswordSecretPath).Trim();
        connectionString += $"Password={password};";
    }
    // If running locally with dotnet run, User Secrets already loaded by ASP.NET Core
    
    return connectionString;
}
```

**Key Point:** User Secrets are loaded automatically by ASP.NET Core's configuration system in Development. Docker Secrets are manually read from `/run/secrets/` when present.

---

## Troubleshooting

### "My User Secrets aren't working in Docker"

**Problem:** User Secrets only work with `dotnet run`, not Docker.

**Solution:** Use Docker Secrets instead:
```bash
mkdir -p secrets
echo -n "YOUR_PASSWORD" > secrets/db_password.txt
docker-compose down && docker-compose up -d --build
```

---

### "My Docker Secrets aren't working locally"

**Problem:** Docker Secrets only work when running with Docker.

**Solution:** Use User Secrets instead:
```bash
cd AIDungeonPromptsWeb
dotnet user-secrets set "ConnectionStrings:AIDungeonPrompt" "Host=localhost;Port=5432;Database=aidungeonprompts;Username=aidungeonprompts_user;Password=YOUR_PASSWORD;"
dotnet run
```

---

### "I setup User Secrets but still getting connection errors in Docker"

**Problem:** Docker doesn't use User Secrets, it needs Docker Secrets.

**Solution:** 
```bash
# Check if Docker Secrets are configured
ls -la secrets/

# If not, create them
mkdir -p secrets
echo -n "YOUR_PASSWORD" > secrets/db_password.txt
echo -n "YOUR_PASSWORD" > secrets/serilog_db_password.txt
chmod 600 secrets/*.txt

# Restart containers
docker-compose down && docker-compose up -d
```

---

### "Which one should I use?"

**Answer:** 
- Running `docker-compose up`? → **Docker Secrets**
- Running `dotnet run`? → **User Secrets**
- When in doubt, use **Docker Secrets** (works for production)

---

## Best Practices

### ✅ DO:
- Use Docker Secrets for all Docker deployments
- Use User Secrets for local development (optional)
- Keep both types of secrets OUT of git
- Use different passwords for dev vs production
- Document which secrets your team uses

### ❌ DON'T:
- Don't commit User Secrets to git (they're in your user profile anyway)
- Don't commit Docker Secrets to git (add `secrets/` to .gitignore)
- Don't use User Secrets in production
- Don't expect User Secrets to work in Docker
- Don't mix them up in documentation

---

## Quick Decision Tree

```
Are you running the application?
├─ With Docker (`docker-compose up`)
│  └─ Use: Docker Secrets
│     └─ Guide: Setup/SetupDockerSecrets.md
│
└─ With dotnet (`dotnet run`)
   └─ Use: User Secrets
      └─ Guide: Setup/UserSecretsSetup.md
```

---

## Summary

| Question | Answer |
|----------|--------|
| **For production?** | Docker Secrets |
| **For Docker deployment?** | Docker Secrets |
| **For local dev with `dotnet run`?** | User Secrets |
| **Default for this project?** | Docker Secrets (most users) |
| **Can I use both?** | Yes, they don't conflict |
| **Which is more common?** | Docker Secrets (90% of users) |
| **Do they conflict?** | No, they work in different scenarios |

---

## Additional Resources

- **Docker Secrets Guide:** `Setup/SetupDockerSecrets.md`
- **User Secrets Guide:** `Setup/UserSecretsSetup.md`
- **Configuration Overview:** `Setup/CONFIGURATION_DEPLOYMENT_IMPROVEMENTS.md`
- **Official Docker Docs:** https://docs.docker.com/engine/swarm/secrets/
- **Official User Secrets Docs:** https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets

---

**Last Updated:** October 2025  
**Version:** 2.1.0  
**Feedback:** If this is still confusing, please let us know so we can improve it!
