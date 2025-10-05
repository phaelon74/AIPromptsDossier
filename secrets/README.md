# Docker Configuration

**NOTE:** This project now uses environment variables for database passwords instead of Docker secrets files.

## Configuration Method

Passwords are configured directly in `docker-compose.yml` as environment variables:

```yaml
environment:
  - POSTGRES_PASSWORD=your_secure_database_password_here  # For db service
  - DB_PASSWORD=your_secure_database_password_here        # For app service
```

## Setup Instructions

1. **Edit `docker-compose.yml`** and replace `your_secure_database_password_here` with your actual database password in **both** places:
   - Line ~13: `POSTGRES_PASSWORD` for the database container
   - Line ~37: `DB_PASSWORD` for the application container

2. **Use the same password** for both variables

3. **Never commit passwords to version control** - add `docker-compose.yml` to `.gitignore` or use environment variable substitution

## Better Security (Optional)

For production, use a `.env` file instead:

1. Create a `.env` file in the project root:
```bash
DB_PASSWORD=your_secure_database_password_here
```

2. Update `docker-compose.yml` to use the variable:
```yaml
environment:
  - POSTGRES_PASSWORD=${DB_PASSWORD}
  - DB_PASSWORD=${DB_PASSWORD}
```

3. Add `.env` to `.gitignore`

## Generate Secure Password

```bash
# Generate a strong random password
openssl rand -base64 32
```
