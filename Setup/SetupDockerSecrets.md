# Docker Secrets Setup Guide

This guide explains how to set up Docker Secrets for the AI Dungeon Prompts application to securely manage database credentials.

## Overview

Docker Secrets provide a secure way to store sensitive information like passwords without hardcoding them in configuration files or docker-compose.yml.

## Prerequisites

- Docker Engine with Swarm mode enabled (or Docker Compose v3.1+)
- PostgreSQL installed and running on the host system
- Database created using the `CreateDatabase.sql` script

## Step 1: Create the Secrets Directory

In your project root, create a directory to store secret files:

```bash
mkdir -p secrets
```

**IMPORTANT:** Add this directory to `.gitignore` to prevent committing secrets to version control!

```bash
echo "secrets/" >> .gitignore
```

## Step 2: Create Database Password Secret

Create a file containing the database password for the application user:

```bash
# Replace YOUR_SECURE_PASSWORD with the actual password you set in CreateDatabase.sql
echo -n "YOUR_SECURE_PASSWORD" > secrets/db_password.txt
```

**Note:** The `-n` flag prevents adding a newline character at the end.

## Step 3: Create Serilog Database Password Secret

Create a file for the Serilog logging password (same as main database password):

```bash
# Use the same password as above
echo -n "YOUR_SECURE_PASSWORD" > secrets/serilog_db_password.txt
```

## Step 4: Set Proper File Permissions

Restrict access to the secret files:

```bash
chmod 600 secrets/db_password.txt
chmod 600 secrets/serilog_db_password.txt
```

## Step 5: Verify Secrets Configuration

Your `docker-compose.yml` should include the following secrets configuration:

```yaml
services:
  app:
    secrets:
      - db_password
      - serilog_db_password

secrets:
  db_password:
    file: ./secrets/db_password.txt
  serilog_db_password:
    file: ./secrets/serilog_db_password.txt
```

## Step 6: Create Backup Directory

Create the directory for database backups on the host:

```bash
sudo mkdir -p /media/main/AIPromptDossier/backups
sudo chown -R $USER:$USER /media/main/AIPromptDossier/backups
chmod 755 /media/main/AIPromptDossier/backups
```

## Step 7: Start the Application

Start the application using Docker Compose:

```bash
docker-compose up -d
```

## How It Works

When the container starts:

1. Docker mounts the secret files inside the container at `/run/secrets/`
2. The application reads from `/run/secrets/db_password` and `/run/secrets/serilog_db_password`
3. These passwords are appended to the connection strings defined in `appsettings.json`

## Security Best Practices

### ✅ DO:
- Keep secret files outside of version control
- Use strong, unique passwords (minimum 16 characters)
- Restrict file permissions (600 or 400)
- Rotate passwords periodically
- Use different passwords for production and development
- Backup secrets securely (encrypted storage)

### ❌ DON'T:
- Commit secret files to Git
- Share secret files via email or messaging
- Use simple or default passwords
- Store secrets in environment variables visible in `docker ps`
- Leave secrets world-readable

## Changing Passwords

If you need to change the database password:

1. **Update PostgreSQL:**
   ```sql
   ALTER USER aidungeonprompts_user WITH PASSWORD 'new_secure_password';
   ```

2. **Update secret files:**
   ```bash
   echo -n "new_secure_password" > secrets/db_password.txt
   echo -n "new_secure_password" > secrets/serilog_db_password.txt
   ```

3. **Restart the application:**
   ```bash
   docker-compose down
   docker-compose up -d
   ```

## Troubleshooting

### Connection Refused Error

**Problem:** Application can't connect to PostgreSQL

**Solutions:**
- Verify PostgreSQL is running: `sudo systemctl status postgresql`
- Check PostgreSQL is listening on 127.0.0.1: `sudo netstat -plnt | grep 5432`
- Ensure `network_mode: "host"` is set in docker-compose.yml
- Verify PostgreSQL allows local connections in `pg_hba.conf`

### Authentication Failed Error

**Problem:** Password authentication fails

**Solutions:**
- Verify secret files contain the correct password (no extra spaces or newlines)
- Check the password in PostgreSQL matches the secret files
- Ensure file permissions are correct (600)
- Check the username is correct: `aidungeonprompts_user`

### Secret File Not Found

**Problem:** Docker can't find secret files

**Solutions:**
- Verify files exist: `ls -la secrets/`
- Check paths in docker-compose.yml are correct
- Ensure you're running docker-compose from the project root

### Permission Denied on Backup Directory

**Problem:** Application can't write to backup directory

**Solutions:**
- Verify directory exists: `ls -la /media/main/AIPromptDossier/`
- Check ownership: `sudo chown -R $USER:$USER /media/main/AIPromptDossier/backups`
- Verify permissions: `chmod 755 /media/main/AIPromptDossier/backups`

## PostgreSQL Configuration

Ensure your PostgreSQL `pg_hba.conf` includes:

```
# TYPE  DATABASE        USER                    ADDRESS         METHOD
local   aidungeonprompts aidungeonprompts_user                  md5
host    aidungeonprompts aidungeonprompts_user  127.0.0.1/32   md5
host    aidungeonprompts aidungeonprompts_user  ::1/128        md5
```

After modifying `pg_hba.conf`, reload PostgreSQL:

```bash
sudo systemctl reload postgresql
```

## Verifying Setup

Test the database connection:

```bash
psql -h 127.0.0.1 -U aidungeonprompts_user -d aidungeonprompts
```

Check container logs:

```bash
docker-compose logs -f app
```

## Production Considerations

For production environments, consider:

1. **Use Docker Swarm Secrets:** More secure than file-based secrets
2. **External Secret Management:** AWS Secrets Manager, Azure Key Vault, HashiCorp Vault
3. **SSL/TLS Connections:** Enable SSL for PostgreSQL connections
4. **Firewall Rules:** Restrict PostgreSQL access to localhost only
5. **Regular Backups:** Automate database backups
6. **Monitoring:** Set up alerts for authentication failures

## Additional Resources

- [Docker Secrets Documentation](https://docs.docker.com/engine/swarm/secrets/)
- [PostgreSQL Security](https://www.postgresql.org/docs/current/auth-pg-hba-conf.html)
- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)

## Support

If you encounter issues not covered in this guide, check the application logs:

```bash
docker-compose logs -f app
```

Or check PostgreSQL logs:

```bash
sudo tail -f /var/log/postgresql/postgresql-*.log
```
