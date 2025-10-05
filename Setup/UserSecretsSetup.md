# User Secrets Setup for Development

This guide explains how to use ASP.NET Core User Secrets for storing sensitive configuration in **LOCAL DEVELOPMENT**.

---

## ⚠️ IMPORTANT: User Secrets vs Docker Secrets

**This guide is ONLY for local development when running `dotnet run` on your machine.**

| Scenario | Use This | Documentation |
|----------|----------|---------------|
| 🐳 **Running with Docker** (`docker-compose up`) | **Docker Secrets** | `Setup/SetupDockerSecrets.md` ✅ |
| 💻 **Local development** (`dotnet run`) | **User Secrets** | This guide ✅ |

**If you're using Docker (most production deployments), you DON'T need User Secrets.**  
**Docker containers use Docker Secrets instead (configured in `docker-compose.yml`).**

---

## Overview

**Problem:** Connection strings with passwords should NOT be committed to source control.

**Solution for Local Dev:** Use User Secrets to store sensitive data locally on your development machine.

---

## What Are User Secrets?

User Secrets is a built-in ASP.NET Core feature that stores sensitive data outside your project directory:

- **Location:** `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json` (Windows)
- **Location:** `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json` (Linux/Mac)
- **Not committed to source control**
- **Only for development** (not for production)

---

## Setup Instructions

### Method 1: Visual Studio (Recommended)

1. **Right-click on `AIDungeonPromptsWeb` project**
2. **Select "Manage User Secrets"**
3. **Add your connection string:**

```json
{
  "ConnectionStrings": {
    "AIDungeonPrompt": "Host=localhost;Port=5432;Database=aidungeonprompts;Username=aidungeonprompts_user;Password=YOUR_DEV_PASSWORD;"
  }
}
```

4. **Save the file**

---

### Method 2: .NET CLI

#### Step 1: Initialize User Secrets

```bash
cd AIDungeonPromptsWeb
dotnet user-secrets init
```

This creates a `UserSecretsId` in your `.csproj` file (if not already present).

#### Step 2: Set Connection String

```bash
dotnet user-secrets set "ConnectionStrings:AIDungeonPrompt" "Host=localhost;Port=5432;Database=aidungeonprompts;Username=aidungeonprompts_user;Password=YOUR_DEV_PASSWORD;"
```

#### Step 3: Verify

```bash
dotnet user-secrets list
```

**Expected output:**
```
ConnectionStrings:AIDungeonPrompt = Host=localhost;Port=5432;Database=aidungeonprompts;Username=aidungeonprompts_user;Password=YOUR_DEV_PASSWORD;
```

---

### Method 3: Manual File Creation

#### Step 1: Find Your UserSecretsId

Open `AIDungeonPromptsWeb/AIDungeonPrompts.Web.csproj` and look for:

```xml
<PropertyGroup>
  <UserSecretsId>YOUR-GUID-HERE</UserSecretsId>
</PropertyGroup>
```

If not present, run `dotnet user-secrets init` first.

#### Step 2: Create Secrets File

**Windows:**
```
%APPDATA%\Microsoft\UserSecrets\YOUR-GUID-HERE\secrets.json
```

**Linux/Mac:**
```
~/.microsoft/usersecrets/YOUR-GUID-HERE/secrets.json
```

#### Step 3: Add Content

```json
{
  "ConnectionStrings": {
    "AIDungeonPrompt": "Host=localhost;Port=5432;Database=aidungeonprompts;Username=aidungeonprompts_user;Password=YOUR_DEV_PASSWORD;"
  }
}
```

---

## Configuration Priority

ASP.NET Core loads configuration in this order (later overrides earlier):

1. `appsettings.json`
2. `appsettings.Development.json`
3. **User Secrets** (Development only)
4. Environment variables
5. Command-line arguments

**Result:** User Secrets will override the password-less connection string in `appsettings.Development.json`.

---

## Verification

### Test Your Configuration

```bash
cd AIDungeonPromptsWeb
dotnet run
```

**Expected behavior:**
- Application starts successfully
- Connects to local PostgreSQL database
- No connection string errors

### Check Connection String

Add temporary logging in `Startup.cs` (remove after testing):

```csharp
public void ConfigureServices(IServiceCollection services)
{
    var connString = Configuration.GetConnectionString("AIDungeonPrompt");
    Console.WriteLine($"Connection String: {connString?.Substring(0, Math.Min(50, connString.Length))}...");
    // ... rest of code
}
```

---

## Security Best Practices

### ✅ DO:
- Use User Secrets for development passwords
- Keep User Secrets file permissions restricted
- Document what secrets are needed (like this guide)
- Use different passwords for dev vs. production

### ❌ DON'T:
- Commit User Secrets to source control
- Share User Secrets file directly
- Use production passwords in development
- Rely on User Secrets in production (use environment variables or Docker Secrets)

---

## Common Issues

### Issue 1: "Connection failed" Error

**Cause:** User Secrets not configured or incorrect password

**Solution:**
1. Verify User Secrets are set: `dotnet user-secrets list`
2. Check PostgreSQL is running: `docker-compose ps` or `systemctl status postgresql`
3. Test connection manually: `psql -U aidungeonprompts_user -h localhost -d aidungeonprompts`

---

### Issue 2: User Secrets Not Loading

**Cause:** Not running in Development environment

**Solution:**
```bash
# Set environment variable
export ASPNETCORE_ENVIRONMENT=Development  # Linux/Mac
set ASPNETCORE_ENVIRONMENT=Development     # Windows CMD
$env:ASPNETCORE_ENVIRONMENT="Development"  # Windows PowerShell

# Then run
dotnet run
```

---

### Issue 3: Can't Find UserSecretsId

**Cause:** User Secrets not initialized

**Solution:**
```bash
cd AIDungeonPromptsWeb
dotnet user-secrets init
```

This adds `<UserSecretsId>` to your `.csproj` file.

---

## Team Onboarding

When new developers join:

1. **Clone repository**
2. **Read this guide**
3. **Setup User Secrets** (using one of the methods above)
4. **Get development database password** from team lead
5. **Test application runs**

---

## Production Deployment

**Important:** User Secrets are **NOT used in production**.

In production, use:
- **Docker Secrets** (containerized deployments)
- **Environment Variables** (cloud deployments)
- **Azure Key Vault** (Azure)
- **AWS Secrets Manager** (AWS)
- **HashiCorp Vault** (enterprise)

See `Setup/SetupDockerSecrets.md` for production configuration.

---

## Example Workflow

### Initial Setup (One Time)

```bash
# Navigate to project
cd AIDungeonPromptsWeb

# Initialize User Secrets
dotnet user-secrets init

# Set database password
dotnet user-secrets set "ConnectionStrings:AIDungeonPrompt" "Host=localhost;Port=5432;Database=aidungeonprompts;Username=aidungeonprompts_user;Password=my_dev_password;"

# Verify
dotnet user-secrets list
```

### Daily Development

```bash
# Just run the application - User Secrets load automatically
dotnet run
```

---

## Additional Secrets

You can store other sensitive development values:

```bash
# API keys
dotnet user-secrets set "ApiKeys:OpenAI" "sk-..."

# Certificates
dotnet user-secrets set "Certificates:Development" "cert-path"

# Test credentials
dotnet user-secrets set "TestUsers:Admin:Password" "test-password"
```

---

## Removing Secrets

### Remove Single Secret

```bash
dotnet user-secrets remove "ConnectionStrings:AIDungeonPrompt"
```

### Clear All Secrets

```bash
dotnet user-secrets clear
```

---

## References

- [Official Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [User Secrets with Visual Studio](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=windows#secret-manager)
- [Configuration in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)

---

## Quick Reference

| Task | Command |
|------|---------|
| Initialize | `dotnet user-secrets init` |
| Set value | `dotnet user-secrets set "Key" "Value"` |
| List all | `dotnet user-secrets list` |
| Remove one | `dotnet user-secrets remove "Key"` |
| Clear all | `dotnet user-secrets clear` |
| Open in editor | Visual Studio: Right-click project → Manage User Secrets |

---

**Last Updated:** October 2025  
**Version:** 2.1.0
