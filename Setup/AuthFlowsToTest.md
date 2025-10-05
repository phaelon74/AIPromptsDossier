# Authentication Flow Testing Guide

This document provides comprehensive testing procedures for all authentication and authorization flows in the AI Dungeon Prompts application.

---

## Table of Contents

1. [User Registration Flow](#1-user-registration-flow)
2. [User Login Flow](#2-user-login-flow)
3. [Account Lockout Flow](#3-account-lockout-flow)
4. [Password Reset Flow](#4-password-reset-flow)
5. [Admin Access Control](#5-admin-access-control)
6. [Session Management](#6-session-management)
7. [First User Admin Assignment](#7-first-user-admin-assignment)
8. [User Enumeration Prevention](#8-user-enumeration-prevention)
9. [Rate Limiting and Backoff](#9-rate-limiting-and-backoff)

---

## Prerequisites

Before testing, ensure:
- ✅ Database is set up and running
- ✅ Application is deployed and accessible
- ✅ You have access to database for verification
- ✅ Browser developer tools are available
- ✅ Multiple browsers/incognito windows for multi-user testing

---

## 1. User Registration Flow

### Test 1.1: Successful Registration - First User (Auto Admin)

**Purpose**: Verify first user is automatically assigned Admin role

**Steps**:
1. Ensure database has NO users: `SELECT COUNT(*) FROM "Users";`
2. Navigate to `/user/register`
3. Fill in registration form:
   - Username: `admin_user`
   - Password: `AdminPass123!`
   - Confirm Password: `AdminPass123!`
4. Click "Register"

**Expected Results**:
- ✅ Redirect to user dashboard
- ✅ User is logged in automatically
- ✅ Database check: `SELECT "Username", "Role" FROM "Users" WHERE "Username" = 'admin_user';`
- ✅ Role should be `8` (Admin)
- ✅ Admin menu/options visible in UI

**Verification Query**:
```sql
SELECT "Id", "Username", "Role", "DateCreated" 
FROM "Users" 
WHERE "Username" = 'admin_user';
```

---

### Test 1.2: Successful Registration - Subsequent Users

**Purpose**: Verify subsequent users get default (None) role

**Steps**:
1. Log out if logged in
2. Navigate to `/user/register`
3. Fill in registration form:
   - Username: `regular_user`
   - Password: `RegularPass123!`
   - Confirm Password: `RegularPass123!`
4. Click "Register"

**Expected Results**:
- ✅ Redirect to user dashboard
- ✅ User is logged in automatically
- ✅ Role should be `0` (None)
- ✅ NO admin menu visible

**Verification Query**:
```sql
SELECT "Id", "Username", "Role", "DateCreated" 
FROM "Users" 
WHERE "Username" = 'regular_user';
```

---

### Test 1.3: Weak Password Rejection

**Purpose**: Verify password policy enforcement

**Test Cases**:

| Password | Expected Result | Error Message |
|----------|----------------|---------------|
| `short` | ❌ Rejected | Password must be at least 12 characters long |
| `alllowercase123!` | ❌ Rejected | Password must contain at least one uppercase letter |
| `ALLUPPERCASE123!` | ❌ Rejected | Password must contain at least one lowercase letter |
| `NoNumbers!` | ❌ Rejected | Password must contain at least one number |
| `NoSpecialChars123` | ❌ Rejected | Password must contain at least one special character |
| `ValidPassword123!` | ✅ Accepted | Registration successful |

**Steps for Each**:
1. Navigate to `/user/register`
2. Enter username: `test_weak_password`
3. Enter test password
4. Enter same password in confirmation
5. Click "Register"
6. Observe error message

---

### Test 1.4: Username Conflict (Generic Error)

**Purpose**: Verify user enumeration prevention

**Steps**:
1. Ensure user `existing_user` exists in database
2. Navigate to `/user/register`
3. Try to register with username: `existing_user`
4. Enter valid password: `ValidPass123!`
5. Click "Register"

**Expected Results**:
- ❌ Registration fails
- ✅ Generic error message: "Registration failed. Please try again with a different username or password."
- ✅ NO message saying "Username already exists"
- ✅ Attacker cannot determine if username exists

---

### Test 1.5: Password Mismatch

**Purpose**: Verify password confirmation validation

**Steps**:
1. Navigate to `/user/register`
2. Username: `mismatch_test`
3. Password: `ValidPass123!`
4. Confirm Password: `DifferentPass123!`
5. Click "Register"

**Expected Results**:
- ❌ Registration fails
- ✅ Error message: "Passwords do not match"
- ✅ User remains on registration page

---

## 2. User Login Flow

### Test 2.1: Successful Login

**Purpose**: Verify successful authentication

**Steps**:
1. Ensure user exists with known credentials
2. Navigate to `/user/login`
3. Enter username: `admin_user`
4. Enter password: `AdminPass123!`
5. Click "Login"

**Expected Results**:
- ✅ Redirect to home page or dashboard
- ✅ User is authenticated (check cookie in dev tools)
- ✅ User menu shows username
- ✅ Logout option available

**Cookie Verification**:
- Check browser dev tools → Application → Cookies
- Should see authentication cookie with HttpOnly flag

---

### Test 2.2: Failed Login - Wrong Password

**Purpose**: Verify incorrect password handling

**Steps**:
1. Navigate to `/user/login`
2. Enter valid username: `admin_user`
3. Enter wrong password: `WrongPassword123!`
4. Click "Login"

**Expected Results**:
- ❌ Login fails
- ✅ Error: "Username or Password was incorrect"
- ✅ User remains on login page
- ✅ Login attempt recorded in database

**Verification Query**:
```sql
SELECT "UserId", "Success", "AttemptDate", "IpAddress"
FROM "LoginAttempts"
WHERE "UserId" = (SELECT "Id" FROM "Users" WHERE "Username" = 'admin_user')
ORDER BY "AttemptDate" DESC
LIMIT 1;
```

Expected: `Success = false`

---

### Test 2.3: Failed Login - Nonexistent User

**Purpose**: Verify user enumeration prevention

**Steps**:
1. Navigate to `/user/login`
2. Enter nonexistent username: `nonexistent_user_xyz`
3. Enter any password: `SomePassword123!`
4. Click "Login"

**Expected Results**:
- ❌ Login fails
- ✅ Same error message: "Username or Password was incorrect"
- ✅ NO indication that user doesn't exist
- ✅ NO login attempt recorded (user doesn't exist)

---

## 3. Account Lockout Flow

### Test 3.1: Failed Attempts 1-5 (No Delay)

**Purpose**: Verify no backoff for first 5 attempts

**Steps**:
1. Create test user or use existing user
2. Attempt login with wrong password 5 times in quick succession
3. Time each attempt

**Expected Results**:
- ✅ All 5 attempts happen immediately (no delay)
- ✅ Error: "Username or Password was incorrect" for each
- ✅ Account NOT locked yet

**Verification Query**:
```sql
SELECT COUNT(*) as failed_attempts
FROM "LoginAttempts"
WHERE "UserId" = (SELECT "Id" FROM "Users" WHERE "Username" = 'test_user')
  AND "Success" = false
  AND "AttemptDate" > NOW() - INTERVAL '15 minutes';
```

Expected: `5` failed attempts

---

### Test 3.2: Failed Attempt 6 (60-Second Backoff)

**Purpose**: Verify backoff timer starts at 6th attempt

**Steps**:
1. After completing Test 3.1 (5 failed attempts)
2. Immediately attempt 6th login with wrong password

**Expected Results**:
- ✅ Login blocked
- ✅ Error message: "Too many failed login attempts. Please wait 60 seconds before trying again."
- ✅ Message includes countdown timer if implemented

**Wait 60 Seconds Then**:
3. Wait 60 seconds
4. Attempt login again with wrong password

**Expected Results**:
- ✅ Attempt is allowed (timer reset)
- ✅ New backoff timer starts for next attempt

---

### Test 3.3: Failed Attempts 7-10 (Continued Backoff)

**Purpose**: Verify backoff continues for attempts 7-10

**Steps**:
1. Repeat login attempts with wrong password for attempts 7, 8, 9, 10
2. Wait 60 seconds between each attempt

**Expected Results**:
- ✅ Each attempt after waiting 60 seconds is processed
- ✅ Backoff message if attempt made before 60 seconds
- ✅ Account still not permanently locked

---

### Test 3.4: Account Lockout After 10 Attempts

**Purpose**: Verify permanent lockout after 10 failed attempts

**Steps**:
1. Complete 10 failed login attempts (following Tests 3.1-3.3)
2. Wait 60 seconds
3. Attempt 11th login with wrong password
4. Attempt login with CORRECT password

**Expected Results**:
- ❌ All login attempts blocked
- ✅ Error: "This account has been locked. Please contact an administrator."
- ✅ Same message even with correct password
- ✅ Account lockout recorded in database

**Verification Query**:
```sql
SELECT "UserId", "LockoutStart", "LockoutEnd", "FailedAttempts", "IsActive"
FROM "AccountLockouts"
WHERE "UserId" = (SELECT "Id" FROM "Users" WHERE "Username" = 'test_user')
  AND "IsActive" = true;
```

Expected: Active lockout with `LockoutEnd = NULL` (permanent)

---

### Test 3.5: Successful Login Resets Failed Attempts

**Purpose**: Verify counter resets on successful login

**Steps**:
1. Create new test user
2. Fail login 3 times
3. Successfully login with correct password
4. Check database

**Expected Results**:
- ✅ Successful login works
- ✅ Failed attempt counter reset
- ✅ No lockout created

**Verification Query**:
```sql
SELECT COUNT(*) as recent_failed_attempts
FROM "LoginAttempts"
WHERE "UserId" = (SELECT "Id" FROM "Users" WHERE "Username" = 'test_user')
  AND "Success" = false
  AND "AttemptDate" > (SELECT MAX("AttemptDate") FROM "LoginAttempts" WHERE "Success" = true AND "UserId" = (SELECT "Id" FROM "Users" WHERE "Username" = 'test_user'));
```

Expected: `0` (counter reset)

---

## 4. Password Reset Flow

### Test 4.1: User Changes Own Password

**Purpose**: Verify password update functionality

**Steps**:
1. Log in as regular user
2. Navigate to `/user/edit`
3. Enter new password: `NewPassword123!`
4. Confirm password: `NewPassword123!`
5. Click "Update"

**Expected Results**:
- ✅ Success message displayed
- ✅ User can log in with new password
- ✅ Old password no longer works

---

### Test 4.2: Password Update with Weak Password

**Purpose**: Verify password policy applies to updates

**Steps**:
1. Log in as user
2. Navigate to `/user/edit`
3. Try to update password to: `weak`
4. Click "Update"

**Expected Results**:
- ❌ Update fails
- ✅ Password policy error messages displayed
- ✅ Password unchanged

---

## 5. Admin Access Control

### Test 5.1: Admin Access to Admin Panel

**Purpose**: Verify admin can access admin features

**Steps**:
1. Log in as user with Admin role (first user created)
2. Navigate to `/admin`

**Expected Results**:
- ✅ Admin panel accessible
- ✅ Can view user list
- ✅ Can see admin-only menu items

---

### Test 5.2: Non-Admin Access Denied

**Purpose**: Verify authorization enforcement

**Steps**:
1. Log in as regular user (non-admin)
2. Attempt to navigate to `/admin`

**Expected Results**:
- ❌ Access denied (403 or redirect)
- ✅ Appropriate error message
- ✅ No admin features visible

---

### Test 5.3: Admin Can Unlock Accounts

**Purpose**: Verify admin unlock functionality

**Steps**:
1. Lock a test account (10 failed logins)
2. Log in as admin
3. Navigate to `/admin/users`
4. Find locked user
5. Click "Unlock Account"

**Expected Results**:
- ✅ Success message displayed
- ✅ Account lockout deactivated in database
- ✅ User can now log in

**Verification Query**:
```sql
SELECT "IsActive" 
FROM "AccountLockouts"
WHERE "UserId" = (SELECT "Id" FROM "Users" WHERE "Username" = 'locked_user');
```

Expected: `IsActive = false`

---

### Test 5.4: Admin Can Reset Passwords

**Purpose**: Verify admin password reset capability

**Steps**:
1. Log in as admin
2. Navigate to `/admin/users`
3. Select a user
4. Reset password to: `AdminReset123!`
5. Click "Reset Password"

**Expected Results**:
- ✅ Success message
- ✅ User can log in with new password
- ✅ Old password no longer works

---

### Test 5.5: Admin Can Assign Roles

**Purpose**: Verify role management

**Steps**:
1. Log in as admin
2. Navigate to `/admin/users`
3. Select regular user
4. Change role to "Delete" (value: 4)
5. Save changes

**Expected Results**:
- ✅ Role updated in database
- ✅ User gains delete permissions after next login

---

## 6. Session Management

### Test 6.1: Session Persistence

**Purpose**: Verify sessions persist correctly

**Steps**:
1. Log in as user
2. Close browser
3. Reopen browser
4. Navigate to application

**Expected Results**:
- ✅ User still logged in (cookie is persistent)
- ✅ Session valid for configured duration (365 days by default)

---

### Test 6.2: Logout Functionality

**Purpose**: Verify logout clears session

**Steps**:
1. Log in as user
2. Click "Logout"
3. Attempt to access protected page

**Expected Results**:
- ✅ Session cleared
- ✅ Cookie removed/invalidated
- ✅ Redirect to login page

---

### Test 6.3: Concurrent Sessions

**Purpose**: Verify multiple session handling

**Steps**:
1. Log in as user in Browser A
2. Log in as same user in Browser B
3. Perform actions in both browsers

**Expected Results**:
- ✅ Both sessions work independently
- ✅ No conflicts or session overwrites

---

## 7. First User Admin Assignment

### Test 7.1: First User Gets Admin Role

**Purpose**: Verify automatic admin assignment

**Steps**:
1. **IMPORTANT**: Start with empty Users table
   ```sql
   DELETE FROM "Users"; -- ONLY in test environment!
   ```
2. Register first user via `/user/register`
3. Check database

**Expected Results**:
- ✅ User created with `Role = 8` (Admin)
- ✅ Admin menu visible immediately after registration

**Verification Query**:
```sql
SELECT "Username", "Role", "DateCreated"
FROM "Users"
ORDER BY "DateCreated" ASC
LIMIT 1;
```

Expected: First user has `Role = 8`

---

### Test 7.2: Second User Does NOT Get Admin

**Purpose**: Verify only first user gets admin

**Steps**:
1. After Test 7.1, register second user
2. Check database

**Expected Results**:
- ✅ Second user has `Role = 0` (None)
- ✅ No admin privileges

---

## 8. User Enumeration Prevention

### Test 8.1: Registration with Existing Username

**Steps**:
1. Try to register with username that exists
2. Observe error message

**Expected Results**:
- ✅ Generic error: "Registration failed. Please try again with a different username or password."
- ❌ NOT: "Username already exists"

---

### Test 8.2: Login with Nonexistent User

**Steps**:
1. Try to log in with username that doesn't exist
2. Observe error message

**Expected Results**:
- ✅ Generic error: "Username or Password was incorrect"
- ❌ NOT: "User not found"

---

### Test 8.3: Password Update with Conflicting Username

**Steps**:
1. Log in as user A
2. Try to change username to user B's username
3. Observe error

**Expected Results**:
- ✅ Generic error: "Update failed. The username or password cannot be used..."
- ❌ NOT: "Username already taken"

---

## 9. Rate Limiting and Backoff

### Test 9.1: Backoff Timer Accuracy

**Purpose**: Verify 60-second timer is accurate

**Steps**:
1. Trigger backoff (6th failed attempt)
2. Wait 30 seconds
3. Try to log in
4. Wait another 30 seconds
5. Try to log in

**Expected Results**:
- ❌ Attempt at 30 seconds blocked
- ✅ Attempt at 60 seconds allowed

---

### Test 9.2: IP Address Tracking

**Purpose**: Verify IP addresses are recorded

**Steps**:
1. Attempt login
2. Check database

**Expected Results**:
- ✅ IP address stored in LoginAttempts table

**Verification Query**:
```sql
SELECT "IpAddress", "AttemptDate", "Success"
FROM "LoginAttempts"
ORDER BY "AttemptDate" DESC
LIMIT 10;
```

---

## Summary Checklist

Before considering authentication testing complete, ensure ALL tests pass:

### Registration
- [ ] First user gets admin role automatically
- [ ] Subsequent users get default role
- [ ] All password policy rules enforced
- [ ] Username conflicts show generic error
- [ ] Password confirmation works

### Login
- [ ] Successful login with correct credentials
- [ ] Failed login with wrong password
- [ ] Failed login with nonexistent user shows same error
- [ ] IP addresses tracked

### Lockout
- [ ] No delay for attempts 1-5
- [ ] 60-second backoff for attempts 6-10
- [ ] Permanent lock after 10 attempts
- [ ] Successful login resets counter

### Admin
- [ ] Admin can access admin panel
- [ ] Non-admin denied access
- [ ] Admin can unlock accounts
- [ ] Admin can reset passwords
- [ ] Admin can assign roles

### Security
- [ ] No user enumeration possible
- [ ] Session management works correctly
- [ ] Logout clears session properly

---

## Automated Testing

For regression testing, consider creating automated tests for these flows:

```bash
# Unit tests
dotnet test AIDungeonPrompts.Test/AuthenticationTests.cs

# Integration tests
dotnet test AIDungeonPrompts.Test/AuthenticationIntegrationTests.cs
```

---

## Troubleshooting

### Tests Failing?

1. **Database not updated**: Run `Setup/CreateDatabase.sql` or `Setup/AddSecurityTables.sql`
2. **Lockout not working**: Check `AccountLockoutService` is registered in DI
3. **Admin role not assigned**: Verify `CreateUserCommandHandler` logic
4. **Backoff not working**: Check system time is correct

### Need to Reset Test Data?

```sql
-- Clear all users (TEST ENVIRONMENT ONLY!)
DELETE FROM "LoginAttempts";
DELETE FROM "AccountLockouts";
DELETE FROM "Users";

-- Reset sequences
ALTER SEQUENCE "Users_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "LoginAttempts_Id_seq" RESTART WITH 1;
ALTER SEQUENCE "AccountLockouts_Id_seq" RESTART WITH 1;
```

---

## Reporting Issues

When reporting authentication bugs, include:
- Test number and description
- Expected vs actual results
- Database state (relevant queries)
- Browser console errors
- Application logs

---

**Last Updated**: October 2025  
**Version**: 1.0  
**Maintainer**: Security Team
