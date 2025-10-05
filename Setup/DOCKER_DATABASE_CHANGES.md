# Docker Database Architecture Changes

This document summarizes the changes made to move PostgreSQL into a Docker container with dedicated networking.

---

## Summary of Changes

### What Changed

**Before:**
- PostgreSQL ran on the host system (outside Docker)
- Application connected via `127.0.0.1:5432`
- Application used `network_mode: "host"` 
- Manual PostgreSQL installation required

**After:**
- PostgreSQL runs in a Docker container
- Application connects via Docker network using hostname `db`
- Dedicated Docker network (`aidungeonprompts_network`)
- Fully containerized deployment

---

## Files Modified

### 1. docker-compose.yml

**Added PostgreSQL Service:**
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
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U aidungeonprompts_user -d aidungeonprompts"]
```

**Updated Application Service:**
- Removed: `network_mode: "host"`
- Added: `ports: - "5001:80"`
- Added: `networks: - db_network`
- Changed connection strings: `Host=127.0.0.1` → `Host=db`
- Added: `depends_on: db (with health check)`

**Added Network:**
```yaml
networks:
  db_network:
    driver: bridge
    name: aidungeonprompts_network
```

### 2. AIDungeonPromptsWeb/appsettings.json

**Changed:**
```diff
- "AIDungeonPrompt": "Host=127.0.0.1;Port=5432;Database=..."
+ "AIDungeonPrompt": "Host=db;Port=5432;Database=..."
```

### 3. AIDungeonPromptsWeb/appsettings.Development.json

**Changed:**
```diff
- "AIDungeonPrompt": "Host=127.0.0.1;Port=5432;..."
+ "AIDungeonPrompt": "Host=localhost;Port=5432;..."
```
*(Uses `localhost` for local development outside Docker)*

---

## New Files Created

1. **Setup/DockerDatabaseSetup.md**
   - Complete guide for containerized PostgreSQL
   - Management commands
   - Troubleshooting
   - Backup/restore procedures

2. **Setup/DOCKER_DATABASE_CHANGES.md** (this file)
   - Summary of architecture changes
   - Migration guide

---

## Architecture Diagram

### Before
```
┌─────────────────────────────────────────────┐
│              Host System                    │
│                                             │
│  ┌──────────────┐      ┌─────────────────┐ │
│  │ PostgreSQL   │◄────►│  Docker App     │ │
│  │ (native)     │      │  (network:host) │ │
│  │ :5432        │      │  :80            │ │
│  └──────────────┘      └─────────────────┘ │
│                                             │
└─────────────────────────────────────────────┘
```

### After
```
┌────────────────────────────────────────────────────┐
│                  Host System                       │
│                                                    │
│   ┌─────────────────────────────────────────────┐ │
│   │    Docker Network: aidungeonprompts_network │ │
│   │                                             │ │
│   │   ┌──────────────┐    ┌─────────────────┐  │ │
│   │   │ PostgreSQL   │◄──►│  Application    │  │ │
│   │   │ Container    │    │  Container      │  │ │
│   │   │ (db:5432)    │    │  (app:80→5001) │  │ │
│   │   └──────┬───────┘    └─────────────────┘  │ │
│   │          │                                  │ │
│   └──────────┼──────────────────────────────────┘ │
│              │                                    │
│      ┌───────▼──────────┐                        │
│      │ /media/main/     │                        │
│      │ AIPromptDossier/ │                        │
│      │   ├── db/        │                        │
│      │   └── backups/   │                        │
│      └──────────────────┘                        │
└────────────────────────────────────────────────────┘
```

---

## Benefits of New Architecture

### Security
✅ **Network Isolation** - Database not exposed to host network  
✅ **Container Isolation** - PostgreSQL runs in isolated environment  
✅ **No Host Dependencies** - Doesn't conflict with host PostgreSQL  

### Portability
✅ **Fully Containerized** - Entire stack runs in Docker  
✅ **Consistent Environments** - Dev/staging/prod parity  
✅ **Easy Deployment** - Single `docker-compose up` command  

### Management
✅ **Centralized Logging** - All logs via Docker  
✅ **Health Checks** - Automatic database health monitoring  
✅ **Easy Backup** - Volume-based backups  
✅ **Version Control** - Easy to upgrade PostgreSQL version  

---

## Migration Guide

### From Host PostgreSQL to Containerized

#### Step 1: Backup Existing Data
```bash
# If you have existing host PostgreSQL
pg_dump -U aidungeonprompts_user -h localhost -d aidungeonprompts > backup_before_migration.sql
```

#### Step 2: Stop Old Setup
```bash
docker-compose down
# Optionally stop host PostgreSQL
sudo systemctl stop postgresql
```

#### Step 3: Create Database Directory
```bash
sudo mkdir -p /media/main/AIPromptDossier/db
sudo chown -R $USER:$USER /media/main/AIPromptDossier/db
chmod 755 /media/main/AIPromptDossier/db
```

#### Step 4: Update Files
- Use new `docker-compose.yml`
- Update `appsettings.json` (already done)
- Ensure Docker Secrets are configured

#### Step 5: Start New Setup
```bash
docker-compose up -d --build
```

#### Step 6: Wait for Database
```bash
# Wait for healthy status
docker-compose ps

# Should show:
# aidungeonprompts_db    ...    Up (healthy)
```

#### Step 7: Initialize or Restore
```bash
# For new installation
docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < Setup/CreateDatabase.sql

# Or restore backup
docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < backup_before_migration.sql
```

#### Step 8: Verify
```bash
# Check application logs
docker-compose logs -f app

# Verify database connection
docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c "\dt"
```

---

## Common Commands

### Database Access
```bash
# Connect to database
docker exec -it aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts

# Run SQL file
docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < script.sql

# Single query
docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c "SELECT version();"
```

### Container Management
```bash
# View all containers
docker-compose ps

# Restart database
docker-compose restart db

# View database logs
docker-compose logs -f db

# Check health
docker inspect aidungeonprompts_db --format='{{.State.Health.Status}}'
```

### Network Testing
```bash
# Test connectivity from app to db
docker exec aidungeonprompts_app ping -c 3 db

# View network details
docker network inspect aidungeonprompts_network

# Check DNS resolution
docker exec aidungeonprompts_app nslookup db
```

---

## Troubleshooting

### Database Won't Start

**Check logs:**
```bash
docker-compose logs db
```

**Common fixes:**
```bash
# Fix permissions
sudo chown -R 999:999 /media/main/AIPromptDossier/db

# Start fresh (⚠️ deletes data!)
docker-compose down
sudo rm -rf /media/main/AIPromptDossier/db/*
docker-compose up -d
```

### Application Can't Connect

**Verify network:**
```bash
docker exec aidungeonprompts_app ping db
```

**Check health:**
```bash
docker-compose ps
# Database should show "Up (healthy)"
```

### Port 5432 Already in Use

If host PostgreSQL is running:
```bash
sudo systemctl stop postgresql
sudo systemctl disable postgresql
```

### Slow Database Performance

**Check resources:**
```bash
docker stats aidungeonprompts_db
```

**Add resource limits in docker-compose.yml:**
```yaml
db:
  deploy:
    resources:
      limits:
        cpus: '2'
        memory: 2G
```

---

## Rollback Procedure

If you need to go back to host PostgreSQL:

1. **Backup containerized data:**
   ```bash
   docker exec aidungeonprompts_db pg_dump -U aidungeonprompts_user -d aidungeonprompts > container_backup.sql
   ```

2. **Stop containers:**
   ```bash
   docker-compose down
   ```

3. **Start host PostgreSQL:**
   ```bash
   sudo systemctl start postgresql
   ```

4. **Restore data:**
   ```bash
   psql -U aidungeonprompts_user -h localhost -d aidungeonprompts < container_backup.sql
   ```

5. **Revert docker-compose.yml** to previous version

6. **Update appsettings.json:**
   ```json
   "AIDungeonPrompt": "Host=127.0.0.1;..."
   ```

---

## Testing Checklist

After migration, verify:

- [ ] Both containers start successfully
- [ ] Database health check passes
- [ ] Application connects to database
- [ ] Can access application at http://localhost:5001
- [ ] Can register new user
- [ ] Can login with existing users
- [ ] Admin panel accessible
- [ ] All data intact (check user count, prompts, etc.)
- [ ] Backup location works
- [ ] Logs are accessible via `docker-compose logs`

---

## Performance Considerations

### Before (Host PostgreSQL)
- Direct access to host resources
- Shared host kernel
- No containerization overhead

### After (Containerized)
- Slight overhead from containerization (~2-5%)
- Better isolation and security
- More manageable resource limits
- Overall performance impact: **Negligible for typical workloads**

---

## Security Improvements

| Aspect | Host PostgreSQL | Containerized |
|--------|----------------|---------------|
| Network Exposure | Bound to 127.0.0.1 | Internal Docker network only |
| Access Control | Host firewall | Docker network + firewall |
| Isolation | System-level | Container-level |
| Secrets | Environment vars | Docker Secrets |
| Updates | Manual | Container image updates |
| Monitoring | System tools | Docker health checks |

---

## Future Enhancements

### Possible Improvements
- [ ] PostgreSQL replication for HA
- [ ] Connection pooling (PgBouncer)
- [ ] Automated backups to S3/cloud storage
- [ ] Prometheus monitoring
- [ ] Read replicas for scaling
- [ ] Custom PostgreSQL tuning

### Multi-Node Setup (Future)
```yaml
# Example: Primary-Replica setup
db_primary:
  image: postgres:14-alpine
  # ... primary config

db_replica:
  image: postgres:14-alpine
  # ... replica config with streaming replication
```

---

## Additional Resources

- **Docker Networking**: https://docs.docker.com/network/
- **PostgreSQL in Docker**: https://hub.docker.com/_/postgres
- **Docker Secrets**: https://docs.docker.com/engine/swarm/secrets/
- **Health Checks**: https://docs.docker.com/engine/reference/builder/#healthcheck

---

## Support

For issues related to the containerized database:
1. Check [Setup/DockerDatabaseSetup.md](DockerDatabaseSetup.md)
2. Review logs: `docker-compose logs db`
3. Test connectivity: `docker exec aidungeonprompts_app ping db`
4. Verify health: `docker-compose ps`

---

**Last Updated:** October 2025  
**Version:** 2.0  
**Migration Status:** Complete
