# Backup Management Guide

This guide explains how to manage backups for the AI Dungeon Prompts application, including database backups and the automated backup system.

---

## Table of Contents

1. [Backup Overview](#backup-overview)
2. [Backup Directory Setup](#backup-directory-setup)
3. [Automated Backup System](#automated-backup-system)
4. [Manual Backup Procedures](#manual-backup-procedures)
5. [Restore Procedures](#restore-procedures)
6. [Backup Monitoring](#backup-monitoring)
7. [Backup Retention Policy](#backup-retention-policy)
8. [Troubleshooting](#troubleshooting)

---

## Backup Overview

The application uses a two-tiered backup strategy:

1. **SQLite Backup Database** - Automated exports of PostgreSQL data to SQLite format
2. **PostgreSQL Native Backups** - Manual or automated pg_dump backups

### Why Two Formats?

- **SQLite**: Portable, single-file format stored in `/AIPromptDossier/backups/backup.db`
- **PostgreSQL**: Native format for full database restoration

---

## Backup Directory Setup

### 1. Create the Backup Directory

The backup directory must exist on the host system and be accessible to the Docker container:

```bash
# Create the directory
sudo mkdir -p /media/main/AIPromptDossier/backups

# Set ownership (replace $USER with your username if needed)
sudo chown -R $USER:$USER /media/main/AIPromptDossier

# Set permissions (755 = owner full access, others read/execute)
chmod 755 /media/main/AIPromptDossier
chmod 755 /media/main/AIPromptDossier/backups
```

### 2. Verify Directory Permissions

```bash
ls -la /media/main/AIPromptDossier/
```

Expected output:
```
drwxr-xr-x  3 your_user your_user 4096 Oct  5 12:00 backups
```

### 3. Test Write Access

```bash
# Test writing to the directory
touch /media/main/AIPromptDossier/backups/test.txt
rm /media/main/AIPromptDossier/backups/test.txt
```

If you can create and delete the test file, permissions are correct.

---

## Automated Backup System

The application includes three automated backup services:

### 1. DatabaseBackupHostedService

**Purpose**: Runs once at application startup  
**Function**: Creates initial SQLite backup from PostgreSQL  
**Location**: `AIDungeonPromptsWeb/HostedServices/DatabaseBackupHostedService.cs`

**Verification**:
```bash
# Check if backup was created at startup
ls -lh /media/main/AIPromptDossier/backups/backup.db
```

### 2. DatabaseBackupCronJob

**Purpose**: Scheduled periodic backups  
**Function**: Backs up PostgreSQL data to SQLite on a schedule  
**Schedule**: Configured in code (check source for exact timing)  
**Location**: `AIDungeonPromptsWeb/HostedServices/DatabaseBackupCronJob.cs`

### 3. ApplicationLogCleanerCronJob

**Purpose**: Maintains log size  
**Function**: Removes old application logs to prevent unbounded growth  
**Location**: `AIDungeonPromptsWeb/HostedServices/ApplicationLogCleanerCronJob.cs`

### Monitoring Automated Backups

Check application logs to verify backups are running:

```bash
# View recent logs
docker-compose logs -f app | grep -i backup

# Look for messages like:
# "DatabaseBackupHostedService Running Job"
# "DatabaseBackupHostedService Job Complete"
```

---

## Manual Backup Procedures

### PostgreSQL Full Backup (Recommended)

**When to use**: 
- Before major updates
- Before data migration
- Regular scheduled backups
- Before risky operations

#### Full Database Backup

```bash
# Backup entire database
pg_dump -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts -F c -b -v -f "/media/main/AIPromptDossier/backups/aidungeonprompts_$(date +%Y%m%d_%H%M%S).backup"

# With compression
pg_dump -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts -F c -b -v -Z 9 -f "/media/main/AIPromptDossier/backups/aidungeonprompts_$(date +%Y%m%d_%H%M%S).backup"
```

#### SQL Format Backup (Human Readable)

```bash
# Create SQL dump
pg_dump -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts > "/media/main/AIPromptDossier/backups/aidungeonprompts_$(date +%Y%m%d_%H%M%S).sql"

# With compression
pg_dump -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts | gzip > "/media/main/AIPromptDossier/backups/aidungeonprompts_$(date +%Y%m%d_%H%M%S).sql.gz"
```

#### Schema Only Backup

```bash
pg_dump -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts --schema-only -f "/media/main/AIPromptDossier/backups/schema_$(date +%Y%m%d).sql"
```

#### Data Only Backup

```bash
pg_dump -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts --data-only -f "/media/main/AIPromptDossier/backups/data_$(date +%Y%m%d).sql"
```

#### Specific Table Backup

```bash
# Backup just Users table
pg_dump -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts -t "Users" > "/media/main/AIPromptDossier/backups/users_$(date +%Y%m%d).sql"
```

---

## Restore Procedures

### Restore from PostgreSQL Backup

#### Full Database Restore

```bash
# Stop the application first
docker-compose down

# Drop and recreate database (CAUTION!)
psql -U postgres << EOF
DROP DATABASE IF EXISTS aidungeonprompts;
CREATE DATABASE aidungeonprompts;
GRANT ALL PRIVILEGES ON DATABASE aidungeonprompts TO aidungeonprompts_user;
EOF

# Restore from custom format backup
pg_restore -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts -v "/media/main/AIPromptDossier/backups/aidungeonprompts_20251005_120000.backup"

# Or restore from SQL dump
psql -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts < "/media/main/AIPromptDossier/backups/aidungeonprompts_20251005_120000.sql"

# Restart application
docker-compose up -d
```

#### Restore Specific Table

```bash
# Restore just one table (doesn't drop database)
pg_restore -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts -t "Users" "/media/main/AIPromptDossier/backups/aidungeonprompts_20251005_120000.backup"
```

### Restore from SQLite Backup

The SQLite backup (`backup.db`) is primarily for the application's internal use. To restore data from it:

1. **Export data from SQLite**:
```bash
sqlite3 /media/main/AIPromptDossier/backups/backup.db << EOF
.mode csv
.headers on
.output /tmp/users_export.csv
SELECT * FROM Users;
.quit
EOF
```

2. **Import into PostgreSQL**:
```sql
\copy "Users" FROM '/tmp/users_export.csv' WITH (FORMAT csv, HEADER true);
```

---

## Backup Monitoring

### Check Backup File Sizes

```bash
# List all backups with sizes
ls -lh /media/main/AIPromptDossier/backups/

# Show total backup directory size
du -sh /media/main/AIPromptDossier/backups/

# Count number of backup files
find /media/main/AIPromptDossier/backups/ -name "*.backup" | wc -l
```

### Verify Backup Integrity

```bash
# Test that backup file is valid
pg_restore --list /media/main/AIPromptDossier/backups/aidungeonprompts_20251005_120000.backup > /dev/null
echo $?  # Should output 0 for success
```

### Check Backup Age

```bash
# Find backups older than 30 days
find /media/main/AIPromptDossier/backups/ -name "*.backup" -mtime +30 -ls

# Find backups created in last 24 hours
find /media/main/AIPromptDossier/backups/ -name "*.backup" -mtime -1 -ls
```

---

## Backup Retention Policy

### Recommended Retention Schedule

| Backup Type | Retention Period | Frequency |
|------------|------------------|-----------|
| Daily Backups | 7 days | Every 24 hours |
| Weekly Backups | 4 weeks | Every Sunday |
| Monthly Backups | 12 months | 1st of month |
| Yearly Backups | Indefinite | January 1st |

### Automated Cleanup Script

Create a cleanup script to maintain retention policy:

```bash
#!/bin/bash
# /media/main/AIPromptDossier/scripts/cleanup_old_backups.sh

BACKUP_DIR="/media/main/AIPromptDossier/backups"
DAYS_TO_KEEP=30

echo "Cleaning up backups older than $DAYS_TO_KEEP days..."

# Delete old .backup files
find "$BACKUP_DIR" -name "*.backup" -type f -mtime +$DAYS_TO_KEEP -delete

# Delete old .sql files
find "$BACKUP_DIR" -name "*.sql" -type f -mtime +$DAYS_TO_KEEP -delete

# Delete old compressed backups
find "$BACKUP_DIR" -name "*.sql.gz" -type f -mtime +$DAYS_TO_KEEP -delete

echo "Cleanup complete. Remaining backups:"
ls -lh "$BACKUP_DIR"
```

Make it executable:
```bash
chmod +x /media/main/AIPromptDossier/scripts/cleanup_old_backups.sh
```

### Schedule with Cron

```bash
# Edit crontab
crontab -e

# Add this line to run cleanup daily at 2 AM
0 2 * * * /media/main/AIPromptDossier/scripts/cleanup_old_backups.sh >> /var/log/backup_cleanup.log 2>&1
```

---

## Automated Backup Script

### Daily Backup Script

```bash
#!/bin/bash
# /media/main/AIPromptDossier/scripts/daily_backup.sh

BACKUP_DIR="/media/main/AIPromptDossier/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
DB_NAME="aidungeonprompts"
DB_USER="aidungeonprompts_user"
DB_HOST="127.0.0.1"

# Full database backup
BACKUP_FILE="$BACKUP_DIR/${DB_NAME}_${TIMESTAMP}.backup"

echo "Starting backup at $(date)"
pg_dump -U $DB_USER -h $DB_HOST -d $DB_NAME -F c -b -v -Z 9 -f "$BACKUP_FILE"

if [ $? -eq 0 ]; then
    echo "Backup successful: $BACKUP_FILE"
    echo "Backup size: $(du -h $BACKUP_FILE | cut -f1)"
else
    echo "Backup FAILED!"
    exit 1
fi

# Verify backup
pg_restore --list "$BACKUP_FILE" > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "Backup verification successful"
else
    echo "Backup verification FAILED!"
    exit 1
fi

echo "Backup complete at $(date)"
```

Make executable:
```bash
chmod +x /media/main/AIPromptDossier/scripts/daily_backup.sh
```

Schedule in cron:
```bash
crontab -e

# Add this line for daily backup at 3 AM
0 3 * * * /media/main/AIPromptDossier/scripts/daily_backup.sh >> /var/log/daily_backup.log 2>&1
```

---

## Disaster Recovery

### Full Disaster Recovery Procedure

1. **Ensure PostgreSQL is running**:
   ```bash
   sudo systemctl start postgresql
   sudo systemctl status postgresql
   ```

2. **Stop the application**:
   ```bash
   docker-compose down
   ```

3. **Recreate database**:
   ```bash
   psql -U postgres << EOF
   DROP DATABASE IF EXISTS aidungeonprompts;
   CREATE DATABASE aidungeonprompts;
   GRANT ALL PRIVILEGES ON DATABASE aidungeonprompts TO aidungeonprompts_user;
   EOF
   ```

4. **Restore from most recent backup**:
   ```bash
   # Find most recent backup
   LATEST_BACKUP=$(ls -t /media/main/AIPromptDossier/backups/*.backup | head -1)
   
   echo "Restoring from: $LATEST_BACKUP"
   pg_restore -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts -v "$LATEST_BACKUP"
   ```

5. **Verify restoration**:
   ```bash
   psql -U aidungeonprompts_user -h 127.0.0.1 -d aidungeonprompts << EOF
   SELECT COUNT(*) as user_count FROM "Users";
   SELECT COUNT(*) as prompt_count FROM "Prompts";
   EOF
   ```

6. **Restart application**:
   ```bash
   docker-compose up -d
   ```

7. **Verify application works**:
   - Navigate to application URL
   - Test login
   - Verify data is present

---

## Troubleshooting

### Backup Directory Permission Denied

**Error**: "Permission denied" when creating backup

**Solution**:
```bash
# Fix permissions
sudo chown -R $USER:$USER /media/main/AIPromptDossier/backups
chmod 755 /media/main/AIPromptDossier/backups
```

### Backup File Not Found

**Error**: Docker container can't find `/AIPromptDossier/backups/`

**Solution**:
```bash
# Verify volume mount in docker-compose.yml
# Should have:
volumes:
  - /media/main/AIPromptDossier/backups:/AIPromptDossier/backups

# Restart container
docker-compose down
docker-compose up -d
```

### Disk Space Full

**Error**: "No space left on device"

**Solution**:
```bash
# Check disk usage
df -h /media/main/

# Remove old backups manually
rm /media/main/AIPromptDossier/backups/old_backup_*.backup

# Or run cleanup script
/media/main/AIPromptDossier/scripts/cleanup_old_backups.sh
```

### Backup Taking Too Long

**Issue**: Backup process is very slow

**Solutions**:
1. Use compression level 6 instead of 9: `-Z 6`
2. Backup during off-peak hours
3. Consider incremental backups
4. Exclude large tables if not critical: `--exclude-table=ApplicationLogs`

### Cannot Restore Backup

**Error**: Various pg_restore errors

**Solutions**:
1. Verify backup file integrity: `pg_restore --list backup.backup`
2. Check PostgreSQL version compatibility
3. Try restoring with verbose mode: `pg_restore -v`
4. Review error messages in detail

---

## Backup Best Practices

### DO ✅

- ✅ Test restore procedures regularly (monthly)
- ✅ Store backups on different physical drive
- ✅ Verify backup integrity after creation
- ✅ Maintain multiple backup generations
- ✅ Document restore procedures
- ✅ Monitor backup disk space
- ✅ Encrypt backups containing sensitive data
- ✅ Test disaster recovery procedures

### DON'T ❌

- ❌ Store backups only on same drive as database
- ❌ Assume backups work without testing restores
- ❌ Keep unlimited old backups (disk space)
- ❌ Run backups during peak traffic
- ❌ Forget to backup after major changes
- ❌ Share unencrypted backups
- ❌ Backup to publicly accessible locations

---

## Off-Site Backup Strategy

For production systems, implement off-site backups:

### Option 1: Cloud Storage (S3, Azure Blob, etc.)

```bash
# Example: Upload to AWS S3
aws s3 cp /media/main/AIPromptDossier/backups/aidungeonprompts_$(date +%Y%m%d).backup \
  s3://your-backup-bucket/aidungeonprompts/ \
  --storage-class STANDARD_IA
```

### Option 2: Remote Server via rsync

```bash
# Sync backups to remote server
rsync -avz --delete /media/main/AIPromptDossier/backups/ \
  user@backup-server:/backups/aidungeonprompts/
```

### Option 3: Network Attached Storage (NAS)

```bash
# Mount NAS
sudo mount -t nfs backup-nas:/volume1/backups /mnt/nas

# Copy backup
cp /media/main/AIPromptDossier/backups/*.backup /mnt/nas/aidungeonprompts/
```

---

## Monitoring and Alerts

### Email Alerts for Backup Failures

Add to backup script:

```bash
# At end of backup script
if [ $BACKUP_SUCCESS -eq 0 ]; then
    echo "Backup completed successfully" | mail -s "Backup Success" admin@example.com
else
    echo "Backup FAILED!" | mail -s "BACKUP FAILURE - URGENT" admin@example.com
fi
```

### Backup Monitoring Dashboard

Create a simple status check:

```bash
#!/bin/bash
# /media/main/AIPromptDossier/scripts/backup_status.sh

echo "=== Backup Status Report ==="
echo "Date: $(date)"
echo ""

echo "Latest Backups:"
ls -lht /media/main/AIPromptDossier/backups/*.backup | head -5

echo ""
echo "Disk Usage:"
du -sh /media/main/AIPromptDossier/backups/

echo ""
echo "Oldest Backup:"
ls -lt /media/main/AIPromptDossier/backups/*.backup | tail -1

echo ""
echo "Backup Count:"
find /media/main/AIPromptDossier/backups/ -name "*.backup" | wc -l
```

---

## Summary Checklist

Before going to production, ensure:

- [ ] Backup directory created with correct permissions
- [ ] Docker volume mount configured correctly
- [ ] Automated backups running successfully
- [ ] Manual backup procedure tested
- [ ] Restore procedure tested and documented
- [ ] Backup retention policy implemented
- [ ] Disk space monitoring in place
- [ ] Off-site backup strategy configured
- [ ] Backup verification automated
- [ ] Disaster recovery plan documented
- [ ] Team trained on restore procedures

---

**Last Updated**: October 2025  
**Version**: 1.0  
**Maintainer**: Operations Team
