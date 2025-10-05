# Docker Secrets

This directory contains sensitive credentials used by the Docker containers.

## Files

- `db_password.txt` - Database password for the application connection
- `serilog_db_password.txt` - Database password for Serilog logging connection

## Setup

1. **Replace the placeholder passwords** in both files with secure passwords
2. Both passwords should typically be the same (the PostgreSQL user password)
3. **Never commit these files to version control** (they're in .gitignore)

## Security Notes

- These files are mounted as Docker secrets at runtime
- The container runs as a non-root user for security
- Keep these files readable only by your user: `chmod 600 secrets/*.txt`
- Use strong, unique passwords for production deployments

## Example

```bash
# Generate a strong password
openssl rand -base64 32 > secrets/db_password.txt
cp secrets/db_password.txt secrets/serilog_db_password.txt

# Set proper permissions
chmod 600 secrets/*.txt
```
