# Setup Documentation

This directory contains all documentation needed to deploy, configure, and maintain the AI Dungeon Prompts application with security enhancements.

## 📚 Documentation Index

### 🔐 Security & Setup
- **[SECURITY_FIXES_SUMMARY.md](SECURITY_FIXES_SUMMARY.md)** - Complete overview of all security fixes implemented
- **[SetupDockerSecrets.md](SetupDockerSecrets.md)** - Docker Secrets configuration guide
- **[DatabaseMigration.md](DatabaseMigration.md)** - Database setup and migration instructions

### 🗄️ Database
- **[CreateDatabase.sql](CreateDatabase.sql)** - Full database creation script for new installations
- **[AddSecurityTables.sql](AddSecurityTables.sql)** - Add security tables to existing database

### 🧪 Testing
- **[AuthFlowsToTest.md](AuthFlowsToTest.md)** - Comprehensive authentication testing procedures

### 💾 Backup & Maintenance
- **[BackupManagement.md](BackupManagement.md)** - Complete backup and disaster recovery guide

---

## 🚀 Quick Start

### For New Installations

1. **Create Required Directories**:
   ```bash
   sudo mkdir -p /media/main/AIPromptDossier/{db,backups}
   sudo chown -R $USER:$USER /media/main/AIPromptDossier
   chmod 755 /media/main/AIPromptDossier/db
   chmod 755 /media/main/AIPromptDossier/backups
   ```

2. **Configure Docker Secrets**:
   ```bash
   mkdir -p secrets
   echo -n "your_secure_password" > secrets/db_password.txt
   echo -n "your_secure_password" > secrets/serilog_db_password.txt
   chmod 600 secrets/*.txt
   ```

3. **Deploy All Containers**:
   ```bash
   docker-compose up -d --build
   ```

4. **Initialize Database Schema**:
   ```bash
   docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < Setup/CreateDatabase.sql
   ```

5. **Register First User** (automatically becomes admin):
   - Navigate to `http://localhost:5001/user/register`
   - Create account with strong password

### For Existing Installations

1. **Create Database Directory**:
   ```bash
   sudo mkdir -p /media/main/AIPromptDossier/db
   sudo chown -R $USER:$USER /media/main/AIPromptDossier/db
   chmod 755 /media/main/AIPromptDossier/db
   ```

2. **Update Configuration**:
   - Follow [SetupDockerSecrets.md](SetupDockerSecrets.md)
   - Use new docker-compose.yml with PostgreSQL container
   - Configure backup and database directories

3. **Deploy Updates**:
   ```bash
   docker-compose down
   docker-compose up -d --build
   ```

4. **Initialize or Restore Database**:
   ```bash
   # For new setup
   docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < Setup/CreateDatabase.sql
   
   # Or restore existing backup
   docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < your_backup.sql
   ```

---

## 📋 Implementation Checklist

### Pre-Deployment
- [ ] Read [SECURITY_FIXES_SUMMARY.md](SECURITY_FIXES_SUMMARY.md)
- [ ] Read [CONFIGURATION_DEPLOYMENT_IMPROVEMENTS.md](CONFIGURATION_DEPLOYMENT_IMPROVEMENTS.md)
- [ ] Docker and Docker Compose installed
- [ ] Database and backup directories created with correct permissions
- [ ] Create appuser (UID 5678) on host system for non-root container
- [ ] Sufficient disk space (10GB+)

### Database Setup
- [ ] PostgreSQL container started via docker-compose
- [ ] Run [CreateDatabase.sql](CreateDatabase.sql) to initialize schema
- [ ] Verify tables created: `docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c "\dt"`
- [ ] Set strong database password in Docker Secrets
- [ ] Test database connection from app container

### Docker Configuration  
- [ ] Docker Secrets configured per [SetupDockerSecrets.md](SetupDockerSecrets.md)
- [ ] secrets/ directory in .gitignore
- [ ] Volume mounts configured in docker-compose.yml
- [ ] Environment variables set correctly

### Application Deployment
- [ ] Application builds successfully
- [ ] Application starts without errors
- [ ] Database connection working
- [ ] Backup directory accessible

### First User Setup
- [ ] First user registered (auto-admin)
- [ ] Can access /admin panel
- [ ] Can perform admin functions

### Testing
- [ ] Follow [AuthFlowsToTest.md](AuthFlowsToTest.md)
- [ ] All authentication flows tested
- [ ] Password policy enforced
- [ ] Account lockout working
- [ ] Admin functions working

### Backup Configuration
- [ ] Read [BackupManagement.md](BackupManagement.md)
- [ ] Automated backups running
- [ ] Manual backup tested
- [ ] Restore procedure tested
- [ ] Retention policy configured

---

## 🔒 Security Features

### Password Security
✅ 12-character minimum  
✅ Uppercase, lowercase, number, special character required  
✅ BCrypt hashing  
✅ Secure password storage  

### Brute Force Protection
✅ Login attempt tracking  
✅ IP address logging  
✅ 60-second backoff (attempts 6-10)  
✅ Account lockout after 10 failed attempts  
✅ Admin unlock capability  

### User Management
✅ First user auto-admin  
✅ Role-based authorization  
✅ Admin dashboard  
✅ Account lockout management  
✅ Password reset capability  

### Data Protection
✅ Database secrets via Docker Secrets  
✅ Backups outside web root  
✅ Secure cookie settings  
✅ HTTPS enforcement  
✅ CSRF protection  

### Attack Prevention
✅ User enumeration prevention  
✅ ZIP path traversal protection  
✅ SQL injection protection (EF Core)  
✅ XSS protection (CSP headers)  

---

## 📖 Document Purposes

| Document | Purpose | When to Read |
|----------|---------|--------------|
| SECURITY_FIXES_SUMMARY.md | Understand what was fixed (Critical) | Before deployment |
| HIGH_PRIORITY_SECURITY_FIXES.md | High-priority fixes (v2.1) | Before deployment |
| MEDIUM_PRIORITY_SECURITY_FIXES.md | Medium-priority fixes (v2.1) | Before deployment |
| CODE_QUALITY_IMPROVEMENTS.md | Code quality enhancements (v2.1) | Code review/maintenance |
| AUDIT_DATABASE_IMPLEMENTATION_PLAN.md | Future audit DB separation plan | Future planning |
| AUTHORIZATION_LOGIC.md | Authorization patterns & testing | Development/Audit |
| DockerDatabaseSetup.md | Containerized PostgreSQL setup | During setup |
| SetupDockerSecrets.md | Configure Docker Secrets (production) | During setup |
| UserSecretsSetup.md | Configure User Secrets (local dev) | Development setup |
| SECRETS_CLARIFICATION.md | ⚠️ User Secrets vs Docker Secrets | **READ FIRST** |
| DatabaseMigration.md | Set up database | During setup |
| CreateDatabase.sql | Create new database | New installation |
| AddSecurityTables.sql | Update existing DB | Upgrading |
| AuthFlowsToTest.md | Test authentication | After deployment |
| BackupManagement.md | Manage backups | Ongoing maintenance |
| DOCKER_DATABASE_CHANGES.md | Database containerization details | Migration |

---

## 🆘 Troubleshooting

### Common Issues

**Database Connection Failed**
- Check database container is healthy: `docker-compose ps`
- View database logs: `docker-compose logs db`
- Test network connectivity: `docker exec aidungeonprompts_app ping db`
- Check Docker Secrets are configured correctly
- Review [DockerDatabaseSetup.md](DockerDatabaseSetup.md) troubleshooting section

**Backup Directory Not Accessible**
- Verify directory exists: `ls -la /media/main/AIPromptDossier/backups/`
- Check permissions: `chmod 755 /media/main/AIPromptDossier/backups`
- Verify Docker volume mount in docker-compose.yml
- See [BackupManagement.md](BackupManagement.md) troubleshooting

**First User Not Admin**
- Check database: `SELECT "Role" FROM "Users" ORDER BY "Id" LIMIT 1;`
- Should be `8` (Admin role)
- If not, manually update: `UPDATE "Users" SET "Role" = 8 WHERE "Id" = 1;`

**Account Lockout Not Working**
- Verify tables exist: `\dt` in psql (should see LoginAttempts, AccountLockouts)
- Check AccountLockoutService is registered in DI
- Review application logs: `docker-compose logs -f app`

**Password Policy Not Enforced**
- Verify validators are registered
- Check FluentValidation is configured in Startup.cs
- Try with different password violations

---

## 🔄 Ongoing Maintenance

### Daily
- Monitor application logs
- Check disk space
- Verify backups are running

### Weekly  
- Review failed login attempts
- Check account lockouts
- Verify backup integrity

### Monthly
- Test backup restore procedure
- Review security logs
- Update admin passwords
- Clean old backups

### Quarterly
- Review and update documentation
- Audit user accounts and roles
- Test disaster recovery plan
- Update dependencies

---

## 📞 Support Resources

### Internal Documentation
- All setup docs in `/Setup` directory
- Application logs: `docker-compose logs -f app`
- Database logs: `/var/log/postgresql/`

### PostgreSQL Commands
```bash
# Connect to database
docker exec -it aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts

# View tables
\dt

# View table structure
\d "TableName"

# Check user roles
SELECT "Username", "Role" FROM "Users";

# Run SQL file
docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < your_script.sql
```

### Docker Commands
```bash
# View logs
docker-compose logs -f app

# Restart application
docker-compose restart app

# Rebuild and restart
docker-compose down && docker-compose up -d --build

# View running containers
docker-compose ps
```

---

## 🎯 Next Steps

After completing initial setup:

1. **Security Hardening**
   - Review [SECURITY_FIXES_SUMMARY.md](SECURITY_FIXES_SUMMARY.md) → "Future Enhancements" section
   - Consider 2FA implementation
   - Set up email notifications for lockouts

2. **Monitoring**
   - Set up log aggregation
   - Configure alerting for failed logins
   - Monitor backup disk usage

3. **Testing**
   - Complete all tests in [AuthFlowsToTest.md](AuthFlowsToTest.md)
   - Document any issues found
   - Create automated tests for regression

4. **Documentation**
   - Customize documents for your environment
   - Add environment-specific notes
   - Train team on procedures

---

## 📝 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Oct 2025 | Initial security fixes implementation |

---

## 👥 Contributors

- Security Team - Initial security audit and fixes
- Development Team - Implementation
- Operations Team - Backup and deployment procedures

---

## 📄 License

See main repository LICENSE file.

---

**Need Help?**

Check the troubleshooting sections in each document, or:
1. Review application logs
2. Check database connectivity  
3. Verify Docker Secrets configuration
4. Consult specific document for your issue
