# AI Dungeon Prompts - Enhanced Security Edition

A community-driven prompt sharing platform for AI Dungeon with enterprise-grade security enhancements.

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![.NET 6.0](https://img.shields.io/badge/.NET-6.0-purple.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-blue.svg)](https://www.postgresql.org/)

**Live Site**: https://aidsprompts.crabdance.com/

---

## 📖 About

This application is a web-based platform for sharing, discovering, and managing AI Dungeon prompts, scenarios, and world information. Users can:

- 📝 **Create and Share Prompts** - Build and publish AI Dungeon scenarios with custom memory, author's notes, and world info
- 🔍 **Search and Discover** - Find prompts by tags, keywords, and filters (NSFW/SFW)
- 🎲 **Random Exploration** - Discover new prompts with the random feature
- 📊 **Track Engagement** - View counts, upvotes, and community feedback
- 📦 **Import/Export** - Support for NovelAI and HoloAI scenario formats
- 🔐 **User Accounts** - Save drafts, manage your prompts, and build a profile
- 👨‍💼 **Admin Tools** - Comprehensive moderation and user management

---

## 🌳 Repository Lineage

This is a **security-hardened fork** with substantial improvements:

```
Original Repository (Woruburu)
    └── https://github.com/Woruburu/AIDungeonPrompts
         │
         └── First Fork (cr4bl1fe) - QoL features and fixes
              └── https://github.com/cr4bl1fe/AIDSPrompts
                   │
                   └── This Repository - Enhanced Security Edition
                        └── https://aidsprompts.crabdance.com/
```

### Credits

- **Original Author**: [Woruburu](https://github.com/Woruburu/AIDungeonPrompts) - Initial application development
- **First Fork**: [cr4bl1fe](https://github.com/cr4bl1fe/AIDSPrompts) - Quality of life improvements
- **This Fork**: Security hardening and enterprise-grade authentication

---

## 🔐 Major Security Enhancements

This fork implements comprehensive security improvements addressing all critical vulnerabilities:

### ✅ Implemented Security Features

| Feature | Status | Description |
|---------|--------|-------------|
| **Strong Password Policy** | ✅ Complete | 12+ characters with complexity requirements |
| **Account Lockout Protection** | ✅ Complete | Progressive backoff (60s) after 5 attempts, permanent lock after 10 |
| **Brute Force Protection** | ✅ Complete | IP tracking, attempt logging, admin unlock capability |
| **User Enumeration Prevention** | ✅ Complete | Generic error messages for registration/login |
| **Docker Secrets Integration** | ✅ Complete | Secure credential management (no hardcoded passwords) |
| **Secure Backup System** | ✅ Complete | Backups stored outside web root with proper permissions |
| **Path Traversal Protection** | ✅ Complete | ZIP file validation preventing malicious uploads |
| **Admin Dashboard** | ✅ Complete | User management, role assignment, account unlocking |
| **First User Auto-Admin** | ✅ Complete | Automatic admin role for initial setup |
| **Role-Based Authorization** | ✅ Complete | Claims-based access control with multiple role flags |

### 📊 Security Statistics

- **7 Critical Vulnerabilities** - Fixed
- **7 High Priority Issues** - Addressed  
- **6 Medium Priority Issues** - Resolved
- **3,050+ Lines** - Of security documentation added

For complete details, see [Setup/SECURITY_FIXES_SUMMARY.md](Setup/SECURITY_FIXES_SUMMARY.md)

---

## 🏗️ Architecture

Built using Clean Architecture principles with .NET 6:

- **Domain Layer** - Entities, enums, and business logic
- **Application Layer** - CQRS pattern with MediatR, FluentValidation
- **Infrastructure Layer** - Identity, authentication services
- **Persistence Layer** - Entity Framework Core, PostgreSQL
- **Web Layer** - ASP.NET Core MVC with Razor views

### Technology Stack

- **Backend**: ASP.NET Core 6.0, C#
- **Database**: PostgreSQL 14+
- **ORM**: Entity Framework Core 6
- **Authentication**: Cookie-based with BCrypt password hashing
- **Validation**: FluentValidation
- **Logging**: Serilog with PostgreSQL sink
- **Containerization**: Docker + Docker Compose
- **Security**: NWebsec, CSRF protection, CSP headers

---

## 🚀 Installation

### Prerequisites

- **Docker** and **Docker Compose** installed
- **Git** for cloning the repository
- **Minimum 4GB RAM** and **10GB disk space**
- **(Optional)** PostgreSQL client tools for database management

### Quick Start (New Installation)

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/AIPromptsDossier.git
   cd AIPromptsDossier
   ```

2. **Create Non-Root Container User** (Security Best Practice)
   ```bash
   # Create user and group with UID/GID 5678 (matches container)
   sudo groupadd -g 5678 appuser
   sudo useradd -u 5678 -g 5678 -M -s /usr/sbin/nologin -c "Docker app user" appuser
   ```

3. **Create Required Directories**
   ```bash
   sudo mkdir -p /media/main/AIPromptDossier/{db,backups}
   
   # Set ownership to appuser (UID 5678) so container can write
   sudo chown -R 5678:5678 /media/main/AIPromptDossier
   sudo chmod 755 /media/main/AIPromptDossier/db
   sudo chmod 755 /media/main/AIPromptDossier/backups
   ```

4. **Configure Docker Secrets**
   ```bash
   mkdir -p secrets
   echo -n "YOUR_SECURE_PASSWORD" > secrets/db_password.txt
   echo -n "YOUR_SECURE_PASSWORD" > secrets/serilog_db_password.txt
   chmod 600 secrets/*.txt
   ```
   
   ⚠️ **Important**: Use a strong password (12+ characters with upper, lower, numbers, special chars)

5. **Add Secrets Directory to .gitignore**
   ```bash
   echo "secrets/" >> .gitignore
   ```

6. **Start All Containers**
   ```bash
   docker-compose up -d --build
   ```
   
   This will:
   - Start PostgreSQL container with persistent storage
   - Start the application container
   - Create a dedicated Docker network for secure communication
   - Wait for database health check before starting app

7. **Initialize Database Schema**
   ```bash
   # Wait for database to be ready (check with docker-compose ps)
   # Then initialize the schema
   docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < Setup/CreateDatabase.sql
   ```

8. **Verify Installation**
   ```bash
   # Check all containers are healthy
   docker-compose ps
   
   # View application logs
   docker-compose logs -f app
   
   # Verify database
   docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c "SELECT COUNT(*) FROM \"Users\";"
   ```

9. **Verify Container Security**
   ```bash
   # Confirm container runs as non-root user
   docker exec aidungeonprompts_app whoami
   # Should output: appuser
   
   docker exec aidungeonprompts_app id
   # Should output: uid=5678(appuser) gid=5678(appuser)
   ```

10. **Create First Admin User**
    - Navigate to: `http://localhost:5001/user/register`
    - Register with a strong password
    - **First user automatically becomes admin**
    - User registration is automatically disabled after first user
    - Access admin panel at: `http://localhost:5001/admin`

### Upgrading from Original Repository

If you're upgrading from the original or cr4bl1fe fork:

1. **Backup Your Existing Database**
   ```bash
   pg_dump -U your_db_user -d your_database > backup_before_upgrade.sql
   ```

2. **Create Non-Root Container User**
   ```bash
   sudo groupadd -g 5678 appuser
   sudo useradd -u 5678 -g 5678 -M -s /usr/sbin/nologin -c "Docker app user" appuser
   ```

3. **Create Required Directories**
   ```bash
   sudo mkdir -p /media/main/AIPromptDossier/{db,backups}
   sudo chown -R 5678:5678 /media/main/AIPromptDossier
   sudo chmod 755 /media/main/AIPromptDossier/db
   sudo chmod 755 /media/main/AIPromptDossier/backups
   ```

4. **Follow Steps 4-6** from Quick Start above (secrets and docker-compose)

5. **Restore Your Data**
   ```bash
   # After containers start, restore your backup
   docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < backup_before_upgrade.sql
   
   # Then add security tables
   docker exec -i aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts < Setup/AddSecurityTables.sql
   ```

6. **Manually Assign Admin Role** to existing user:
   ```bash
   docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c "UPDATE \"Users\" SET \"Role\" = 8 WHERE \"Id\" = 1;"
   ```

### Local Development Setup (Without Docker)

If you want to develop locally using `dotnet run` instead of Docker:

1. **Install Prerequisites**
   - .NET 6.0 SDK
   - PostgreSQL 14+ running locally
   - Visual Studio or VS Code (recommended)

2. **Setup Database**
   ```bash
   # Run as PostgreSQL superuser
   psql -U postgres -f Setup/CreateDatabase.sql
   ```

3. **Configure User Secrets** (NOT Docker Secrets)
   ```bash
   cd AIDungeonPromptsWeb
   
   # Initialize User Secrets
   dotnet user-secrets init
   
   # Set your local database password
   dotnet user-secrets set "ConnectionStrings:AIDungeonPrompt" "Host=localhost;Port=5432;Database=aidungeonprompts;Username=aidungeonprompts_user;Password=YOUR_DEV_PASSWORD;"
   ```
   
   📚 **See:** `Setup/UserSecretsSetup.md` for detailed guide

4. **Run Application**
   ```bash
   dotnet run
   # Application starts on http://localhost:5000
   ```

**Important Notes:**
- **User Secrets** are for local development only (stored in your user profile, not in git)
- **Docker Secrets** are for production Docker deployments (stored in `secrets/` directory)
- Never commit passwords to git in either scenario

---

## 📚 Documentation

Comprehensive documentation is available in the `/Setup` directory:

| Document | Purpose |
|----------|---------|
| **[Setup/README.md](Setup/README.md)** | Master documentation index |
| **[Setup/SECURITY_FIXES_SUMMARY.md](Setup/SECURITY_FIXES_SUMMARY.md)** | Complete security improvements overview |
| **[Setup/HIGH_PRIORITY_SECURITY_FIXES.md](Setup/HIGH_PRIORITY_SECURITY_FIXES.md)** | High-priority vulnerability fixes (v2.1) |
| **[Setup/MEDIUM_PRIORITY_SECURITY_FIXES.md](Setup/MEDIUM_PRIORITY_SECURITY_FIXES.md)** | Medium-priority vulnerability fixes (v2.1) |
| **[Setup/CODE_QUALITY_IMPROVEMENTS.md](Setup/CODE_QUALITY_IMPROVEMENTS.md)** | Code quality enhancements (v2.1) |
| **[Setup/CONFIGURATION_DEPLOYMENT_IMPROVEMENTS.md](Setup/CONFIGURATION_DEPLOYMENT_IMPROVEMENTS.md)** | Configuration and deployment security (v2.1) |
| **[Setup/AUTHORIZATION_LOGIC.md](Setup/AUTHORIZATION_LOGIC.md)** | Authorization patterns and best practices |
| **[Setup/AUDIT_DATABASE_IMPLEMENTATION_PLAN.md](Setup/AUDIT_DATABASE_IMPLEMENTATION_PLAN.md)** | Future: Separate audit database (planned) |
| **[Setup/UserSecretsSetup.md](Setup/UserSecretsSetup.md)** | Development credential management guide |
| **[Setup/SECRETS_CLARIFICATION.md](Setup/SECRETS_CLARIFICATION.md)** | ⚠️ **Important:** User Secrets vs Docker Secrets explained |
| **[Setup/DockerDatabaseSetup.md](Setup/DockerDatabaseSetup.md)** | Containerized PostgreSQL setup and management |
| **[Setup/SetupDockerSecrets.md](Setup/SetupDockerSecrets.md)** | Docker Secrets configuration guide |
| **[Setup/DatabaseMigration.md](Setup/DatabaseMigration.md)** | Database setup and migration |
| **[Setup/AuthFlowsToTest.md](Setup/AuthFlowsToTest.md)** | Authentication testing procedures (35+ test cases) |
| **[Setup/BackupManagement.md](Setup/BackupManagement.md)** | Backup and disaster recovery guide |

---

## 🔧 Configuration

### Environment Variables

Set in `docker-compose.yml` or container environment:

```yaml
environment:
  - ConnectionStrings__AIDungeonPrompt=Host=127.0.0.1;Port=5432;Database=aidungeonprompts;Username=aidungeonprompts_user
  - ASPNETCORE_ENVIRONMENT=Production
```

### Password Policy

Enforced automatically for all users:
- Minimum 12 characters
- At least one uppercase letter (A-Z)
- At least one lowercase letter (a-z)  
- At least one number (0-9)
- At least one special character (!@#$%^&*(),.?":{}|<>)

### Account Lockout Settings

Located in `AccountLockoutService.cs`:
```csharp
private const int MaxFailedAttempts = 10;        // Permanent lockout threshold
private const int BackoffStartAttempt = 5;       // When 60-second delay begins
private const int MinutesBeforeBackoffStarts = 15; // Attempt tracking window
```

---

## 🧪 Testing

### Run All Authentication Tests

Follow the comprehensive test guide:

```bash
# See Setup/AuthFlowsToTest.md for 35+ detailed test cases
```

Test categories include:
- User registration (5 tests)
- User login (3 tests)
- Account lockout (5 tests)
- Password reset (2 tests)
- Admin access control (5 tests)
- Session management (3 tests)
- Security features (8+ tests)

### Automated Testing (Future)

```bash
dotnet test AIDungeonPrompts.Test/
```

---

## 🛡️ Security Best Practices

### For Administrators

- ✅ Use strong, unique passwords for database and admin accounts
- ✅ Regularly review login attempt logs
- ✅ Monitor account lockouts and investigate suspicious patterns
- ✅ Keep backups in secure, off-site location
- ✅ Review user roles and permissions quarterly
- ✅ Enable HTTPS in production (update docker-compose.yml)
- ✅ Set up log monitoring and alerting

### For Developers

- ✅ Never commit secrets to version control
- ✅ Run security scanners regularly
- ✅ Keep dependencies updated
- ✅ Test authentication flows after changes
- ✅ Follow principle of least privilege
- ✅ Document any security-related changes

---

## 🔍 Monitoring

### Check Application Health

```bash
# View logs
docker-compose logs -f app

# Check container status
docker-compose ps

# Monitor disk usage
du -sh /media/main/AIPromptDossier/backups/
```

### Database Queries

```bash
# Connect to database
docker exec -it aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts

# Then run queries:
```

```sql
-- View recent login attempts
SELECT u."Username", la."Success", la."AttemptDate", la."IpAddress"
FROM "LoginAttempts" la
JOIN "Users" u ON la."UserId" = u."Id"
ORDER BY la."AttemptDate" DESC
LIMIT 20;

-- View locked accounts
SELECT u."Username", al."LockoutStart", al."FailedAttempts"
FROM "AccountLockouts" al
JOIN "Users" u ON al."UserId" = u."Id"
WHERE al."IsActive" = true;

-- View user roles
SELECT "Username", "Role", "DateCreated"
FROM "Users"
ORDER BY "DateCreated";
```

---

## 🤝 Contributing

We welcome contributions! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Security Issues

**DO NOT** open public issues for security vulnerabilities. Instead:
- Email security concerns to: [your-security-email]
- Use GitHub Security Advisories (private disclosure)

---

## 🐛 Troubleshooting

### Common Issues

**Database Connection Failed**
```bash
# Check database container is healthy
docker-compose ps

# View database logs
docker-compose logs db

# Verify connection from app container
docker exec aidungeonprompts_app ping db

# Check Docker Secrets are configured
cat secrets/db_password.txt
```

**Permission Denied on Backups**
```bash
# Fix permissions
sudo chown -R $USER:$USER /media/main/AIPromptDossier/backups
chmod 755 /media/main/AIPromptDossier/backups
```

**Account Locked Out**
```bash
# Admin can unlock via web interface at /admin/users
# Or manually via database:
docker exec aidungeonprompts_db psql -U aidungeonprompts_user -d aidungeonprompts -c \
  "UPDATE \"AccountLockouts\" SET \"IsActive\" = false WHERE \"UserId\" = [USER_ID];"
```

For more troubleshooting, see [Setup/SetupDockerSecrets.md](Setup/SetupDockerSecrets.md#troubleshooting)

---

## 📊 Project Statistics

- **Language**: C# 77.9%
- **Frontend**: HTML 16.3%, JavaScript 4.0%, CSS 1.4%
- **Lines of Code**: ~50,000+
- **Security Documentation**: 3,050+ lines
- **Test Cases**: 35+ authentication tests documented

---

## 📄 License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

### License Summary

- ✅ **Commercial use** - Allowed
- ✅ **Modification** - Allowed
- ✅ **Distribution** - Allowed
- ✅ **Patent use** - Allowed
- ⚠️ **Liability** - Limited
- ⚠️ **Warranty** - None
- ❗ **License and copyright notice** - Required
- ❗ **State changes** - Required
- ❗ **Disclose source** - Required
- ❗ **Same license** - Required (copyleft)

---

## 🙏 Acknowledgments

- **[Woruburu](https://github.com/Woruburu)** - Original application architecture and development
- **[cr4bl1fe](https://github.com/cr4bl1fe)** - Quality of life improvements and bug fixes
- **AI Dungeon Community** - Feature requests and testing
- **.NET Community** - Libraries and tools (MediatR, FluentValidation, EF Core)
- **Security Researchers** - Best practices and vulnerability awareness

---

## 📞 Support

- **Documentation**: See `/Setup` directory
- **Issues**: [GitHub Issues](https://github.com/yourusername/AIPromptsDossier/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/AIPromptsDossier/discussions)
- **Live Site**: https://aidsprompts.crabdance.com/

---

## 🗺️ Roadmap

### Completed ✅
- [x] Strong password policy enforcement
- [x] Account lockout and brute force protection  
- [x] Admin dashboard for user management
- [x] Docker Secrets integration
- [x] Secure backup system
- [x] Path traversal protection
- [x] Comprehensive documentation

### Planned 🚧
- [ ] Two-factor authentication (2FA)
- [ ] Email verification for new accounts
- [ ] Email notifications for account lockouts
- [ ] OAuth integration (Google, GitHub)
- [ ] API rate limiting
- [ ] Automated security scanning in CI/CD
- [ ] Elasticsearch for advanced search
- [ ] Redis caching for performance

### Under Consideration 💭
- [ ] Mobile application
- [ ] GraphQL API
- [ ] Multi-language support (i18n)
- [ ] Advanced prompt analytics
- [ ] Social features (following, favorites)

---

## 📈 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.1.0 | Oct 2025 | Containerized PostgreSQL + High-priority security fixes |
| 2.0.0 | Oct 2025 | Security hardening release (Critical vulnerabilities) |
| 1.x | Earlier | Original/fork releases |

---

**Built with ❤️ for the AI Dungeon community**

---

*Last Updated: October 2025*