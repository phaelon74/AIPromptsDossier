# Docker Database Setup Guide

This guide explains the containerized PostgreSQL setup for the AI Dungeon Prompts application.

---

## Overview

PostgreSQL now runs as a Docker container alongside the application, with:
- **Dedicated Docker network** (`aidungeonprompts_network`) for secure inter-container communication
- **Persistent storage** at `/media/main/AIPromptDossier/db/` on the host
- **Health checks** to ensure database is ready before app starts
- **Docker Secrets** for secure credential management

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         Host System                         │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │         Docker Network: aidungeonprompts_network      │ │
│  │                                                       │ │
│  │  ┌─────────────────┐      ┌──────────────────────┐  │ │
│  │  │  PostgreSQL     │      │   Application        │  │ │
│  │  │  Container      │◄────►│   Container          │  │ │
│  │  │  (db)           │      │   (app)              │  │ │
│  │  │                 │      │                      │  │ │
│  │  │  Port: 5432     │      │  Port: 80→5001      │  │ │
│  │  └────────┬────────┘      └──────────┬──────────┘  │ │
│  │           │                          │             │ │
│  └───────────┼──────────────────────────┼─────────────┘ │
│              │                          │               │
│      ┌───────▼──────┐          ┌────────▼────────┐     │
│      │ /media/main/ │          │ /media/main/    │     │
│      │ AIPrompt     │          │ AIPrompt        │     │
│      │ Dossier/db/  │          │ Dossier/backups/│     │
│      └──────────────┘          └─────────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

---

## Prerequisites

1. **Create Database Directory**
   ```bash
   sudo mkdir -p /media/main/AIPromptDossier/db
   sudo chown -R $USER:$USER /media/main/AIPromptDossier
   chmod 755 /media/main/AIPromptDossier/db
   ```

2. **Create Backup Directory** (if not already created)
   ```bash
   sudo mkdir -p /media/main/AIPromptDossier/backups
   chmod 755 /media/main/AIPromptDossier/backups
   ```

3. **Configure Docker Secrets**
   ```bash
   mkdir -p secrets
   echo -n "YOUR_SECURE_PASSWORD" > secrets/db_password.txt
   echo -n "YOUR_SECURE_PASSWORD" > secrets/serilog_db_password.txt
   chmod 600 secrets/*.txt
   ```

---

## Docker Compose Configuration

### Services

#### PostgreSQL Container (`db`)
```yaml
db:
  image: postgres:14-alpine
  container_name: aidungeonprompts_db
  volumes:
    - /media/main/AIPromptDossier/db:/var/lib/postgresql/data
  networks:
    - db_network
  secrets:
    - db_password
```

**Key Features:**
- Uses lightweight Alpine Linux image
- Data persists in `/media/main/AIPromptDossier/db/`
- Connects to dedicated `db_network`
- Password loaded from Docker Secret

#### Application Container (`app`)
```yaml
app:
  image: aidungeonprompts_app
  container_name: aidungeonprompts_app
  environment:
    - ConnectionStrings__AIDungeonPrompt=Host=db;Port=5432;...
  networks:
    - db_network
  depends_on:
    db:
      condition: service_healthy
```

**Key Features:**
- Connects to database using service name `db`
- Waits for database health check before starting
- Shares `db_network` with PostgreSQL

### Network

```yaml
networks:
  db_network:
    driver: bridge
    name: aidungeonprompts_network
```

**Benefits:**
- Isolated network for database communication
- Internal DNS resolution (app → `db` hostname)
- No need for port exposure on host
- Better security isolation

---

## Deployment Steps

### Fresh Installation

1. **Create Required Directories**
   ```bash
   sudo mkdir -p /media/main/AIPromptDossier/{db,backups}
   sudo chown -R $USER:$USER /media/main/AIPromptDossier
   chmod 755 /media/main/AIPromptDossier/db
   chmod 755 /media/main/AIPromptDossier/backups
   ```

2. **Configure Secrets**
   ```bash
   mkdir -p secrets
   echo -n "YourSecurePassword123!" > secrets/db_password.txt
   echo -n "YourSecurePassword123!" > secrets/serilog_db_password.txt
   chmod 600 secrets/*.txt
   echo "secrets/" >> .gitignore
   ```

3. **Start Containers**
   ```bash
   docker-compose up -d
   ```

4. **Wait for Database to Initialize**
   ```bash
   # Watch logs
   docker-compose logs -f db
   
   # Wait for: "database system is ready to accept connections"
   ```

5. **Initialize Database Schema**
   ```bash
   # Connect to database container
   docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < Setup/CreateDatabase.sql
   ```

6. **Verify Application Startup**
   ```bash
   docker-compose logs -f app
   
   # Look for successful database connection messages
   ```

---

## Database Management

### Connect to Database

#### From Host System
```bash
docker exec -it aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts
```

#### Using Docker Compose
```bash
docker-compose exec db psql -U aidungeonprompts_user -d aidungeonprompts
```

### Run SQL Commands

```bash
# Execute SQL file
docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < your_script.sql

# Single query
docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c "SELECT COUNT(*) FROM \"Users\";"
```

### Backup Database

#### Using pg_dump (Recommended)
```bash
# Full backup
docker exec aidungeonprompts_db pg_dump -U aidungeonprompts_user -d aidungeonprompts -F c > backup_$(date +%Y%m%d).backup

# SQL format
docker exec aidungeonprompts_db pg_dump -U aidungeonprompts_user -d aidungeonprompts > backup_$(date +%Y%m%d).sql
```

#### Backup Data Directory
```bash
# Stop containers first
docker-compose down

# Backup entire data directory
sudo tar -czf aidungeonprompts_db_backup_$(date +%Y%m%d).tar.gz /media/main/AIPromptDossier/db/

# Restart
docker-compose up -d
```

### Restore Database

#### From pg_dump Backup
```bash
# Stop app container
docker-compose stop app

# Restore
docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < backup_20251005.sql

# Restart app
docker-compose start app
```

#### From Data Directory Backup
```bash
# Stop all containers
docker-compose down

# Remove current data
sudo rm -rf /media/main/AIPromptDossier/db/*

# Restore backup
sudo tar -xzf aidungeonprompts_db_backup_20251005.tar.gz -C /

# Restart containers
docker-compose up -d
```

---

## Monitoring

### Check Container Health

```bash
# View all containers
docker-compose ps

# Check database health
docker inspect aidungeonprompts_db --format='{{.State.Health.Status}}'

# View health check logs
docker inspect aidungeonprompts_db --format='{{range .State.Health.Log}}{{.Output}}{{end}}'
```

### Database Logs

```bash
# Real-time logs
docker-compose logs -f db

# Last 100 lines
docker-compose logs --tail=100 db

# Logs since specific time
docker-compose logs --since 2023-10-05T10:00:00 db
```

### Resource Usage

```bash
# Container stats
docker stats aidungeonprompts_db aidungeonprompts_app

# Disk usage
docker system df
du -sh /media/main/AIPromptDossier/db/
```

---

## Network Configuration

### View Network Details

```bash
# Inspect network
docker network inspect aidungeonprompts_network

# List containers on network
docker network inspect aidungeonprompts_network --format='{{range .Containers}}{{.Name}} {{end}}'
```

### Test Connectivity

```bash
# From app container to db
docker exec aidungeonprompts_app ping -c 3 db

# DNS resolution test
docker exec aidungeonprompts_app nslookup db

# PostgreSQL connection test
docker exec aidungeonprompts_app nc -zv db 5432
```

---

## Troubleshooting

### Database Won't Start

**Check logs:**
```bash
docker-compose logs db
```

**Common issues:**

1. **Permission Denied**
   ```bash
   sudo chown -R 999:999 /media/main/AIPromptDossier/db
   ```
   (PostgreSQL runs as UID 999 in container)

2. **Port Already in Use**
   ```bash
   # Check if host PostgreSQL is running
   sudo systemctl status postgresql
   sudo systemctl stop postgresql
   ```

3. **Corrupted Data Directory**
   ```bash
   docker-compose down
   sudo rm -rf /media/main/AIPromptDossier/db/*
   docker-compose up -d
   # Re-initialize schema
   ```

### Application Can't Connect to Database

**Check health:**
```bash
docker-compose ps
# Ensure db shows "healthy"
```

**Check network:**
```bash
docker exec aidungeonprompts_app ping db
```

**Check credentials:**
```bash
# Verify secret file exists
cat secrets/db_password.txt

# Check environment variable in container
docker exec aidungeonprompts_app printenv | grep ConnectionStrings
```

### Database Running Out of Disk Space

```bash
# Check disk usage
df -h /media/main/

# Check database size
docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c "SELECT pg_size_pretty(pg_database_size('aidungeonprompts'));"

# Vacuum database
docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c "VACUUM FULL;"
```

### Health Check Failing

```bash
# View health check details
docker inspect aidungeonprompts_db | grep -A 10 "Health"

# Manually run health check command
docker exec aidungeonprompts_db pg_isready -U aidungeonprompts_user -d aidungeonprompts

# Check PostgreSQL is actually running
docker exec aidungeonprompts_db ps aux | grep postgres
```

---

## Maintenance

### Update PostgreSQL Version

```bash
# Backup first!
docker exec aidungeonprompts_db pg_dump -U aidungeonprompts_user -d aidungeonprompts > backup_before_upgrade.sql

# Stop containers
docker-compose down

# Update image in docker-compose.yml (e.g., postgres:15-alpine)

# Pull new image
docker-compose pull db

# Start with new version
docker-compose up -d

# Verify
docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c "SELECT version();"
```

### Optimize Database

```bash
# Analyze and vacuum
docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts << EOF
VACUUM ANALYZE;
REINDEX DATABASE aidungeonprompts;
EOF
```

### Reset Database

```bash
# ⚠️ WARNING: This deletes all data!
docker-compose down
sudo rm -rf /media/main/AIPromptDossier/db/*
docker-compose up -d
# Wait for database to initialize, then run schema
docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < Setup/CreateDatabase.sql
```

---

## Security Best Practices

### ✅ DO:
- Use Docker Secrets for passwords
- Regularly backup the database
- Monitor container logs for suspicious activity
- Keep PostgreSQL image updated
- Use strong passwords (12+ characters)
- Restrict filesystem permissions (755 for directories, 600 for secrets)

### ❌ DON'T:
- Expose PostgreSQL port (5432) to host unless necessary
- Store database password in environment variables directly
- Run containers as root
- Use default or weak passwords
- Share Docker Secrets files
- Commit secrets to version control

---

## Production Considerations

### High Availability

For production, consider:
- PostgreSQL replication (primary-replica setup)
- Connection pooling (PgBouncer)
- Automated backups with retention policy
- Monitoring and alerting (Prometheus + Grafana)
- Resource limits in docker-compose.yml

### Example Resource Limits

```yaml
db:
  deploy:
    resources:
      limits:
        cpus: '2'
        memory: 2G
      reservations:
        cpus: '1'
        memory: 1G
```

---

## Migration from Host PostgreSQL

If migrating from host-based PostgreSQL to containerized:

1. **Backup existing database:**
   ```bash
   pg_dump -U aidungeonprompts_user -h localhost -d aidungeonprompts > migration_backup.sql
   ```

2. **Stop old setup:**
   ```bash
   docker-compose down
   sudo systemctl stop postgresql
   ```

3. **Update configuration:**
   - Use new docker-compose.yml
   - Create directories
   - Configure secrets

4. **Start new setup:**
   ```bash
   docker-compose up -d
   ```

5. **Restore data:**
   ```bash
   docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < migration_backup.sql
   ```

6. **Verify:**
   ```bash
   docker-compose logs -f app
   # Check for successful connections
   ```

---

## Quick Reference

| Task | Command |
|------|---------|
| Start all services | `docker-compose up -d` |
| Stop all services | `docker-compose down` |
| View logs | `docker-compose logs -f` |
| Connect to DB | `docker exec -it aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts` |
| Backup DB | `docker exec aidungeonprompts_db pg_dump -U aidungeonprompts_user -d aidungeonprompts > backup.sql` |
| Restart DB | `docker-compose restart db` |
| Check health | `docker-compose ps` |
| View network | `docker network inspect aidungeonprompts_network` |

---

**Last Updated:** October 2025  
**Version:** 1.0
