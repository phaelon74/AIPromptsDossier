# Authorization Logic Documentation

This document explains the authorization patterns used throughout the application.

---

## Overview

The application uses a **role-based authorization system** combined with **ownership checks** to control access to resources.

---

## Role System

### RoleEnum (Flags Enum)

Located in: `AIDungeonPrompts.Domain/Enums/RoleEnum.cs`

```csharp
[Flags]
public enum RoleEnum
{
    None = 0,
    Delete = 1,
    Edit = 2,
    Create = 4,
    Admin = 8
}
```

**Key Points:**
- Uses `[Flags]` attribute for bitwise operations
- Allows users to have multiple roles simultaneously
- Each role is a power of 2 for efficient bitwise checking

### Checking Roles

```csharp
// Check if user has Delete permission
if ((user.Role & RoleEnum.Delete) != 0)
{
    // User has Delete permission
}

// Check if user has Edit OR Create permission
if ((user.Role & (RoleEnum.Edit | RoleEnum.Create)) != 0)
{
    // User has at least one of these permissions
}
```

---

## Authorization Patterns

### Pattern 1: Ownership-Based Authorization

**Example:** Prompt deletion in `PromptsController.cs:278`

```csharp
if (prompt.OwnerId != user.Id && (user.Role & RoleEnum.Delete) == 0)
{
    return NotFound(); // User doesn't own prompt AND doesn't have Delete role
}
```

**Logic:**
1. User can perform action if they **own the resource** (e.g., `prompt.OwnerId == user.Id`)
2. OR user has the **appropriate role permission** (e.g., `Delete` role)
3. Returns `NotFound()` instead of `Forbidden()` to prevent information disclosure

### Pattern 2: Policy-Based Authorization

**Example:** Admin-only actions

```csharp
[Authorize(Policy = PolicyValueConstants.AdminsOnly)]
public class AdminController : Controller
{
    // All actions require Admin policy
}
```

**Policies Defined in `Startup.cs`:**

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyValueConstants.EditorsOnly, 
        policy => policy.RequireClaim(ClaimValueConstants.CanEdit, true.ToString()));
    
    options.AddPolicy(PolicyValueConstants.AdminsOnly, 
        policy => policy.RequireClaim(ClaimValueConstants.IsAdmin, true.ToString()));
});
```

### Pattern 3: Action-Level Authorization

**Example:** Edit prompt action

```csharp
[HttpGet("/{id:int}/edit")]
[Authorize(Policy = PolicyValueConstants.EditorsOnly)]
public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
{
    // Additional checks inside action
    if (!_currentUserService.TryGetCurrentUser(out GetUserViewModel? user))
    {
        return NotFound();
    }
    
    GetPromptViewModel? prompt = await _mediator.Send(new GetPromptQuery(id.Value), cancellationToken);
    
    if (prompt == null || (prompt.OwnerId != user.Id && !RoleHelper.CanEdit(user.Role)))
    {
        return NotFound();
    }
    
    // User authorized, proceed
}
```

---

## Helper Methods

Located in: `AIDungeonPrompts.Application/Helpers/RoleHelper.cs`

```csharp
public static class RoleHelper
{
    public static bool CanEdit(RoleEnum role)
    {
        return (RoleEnum.Edit & role) != 0;
    }
    
    public static bool IsAdmin(RoleEnum role)
    {
        return (RoleEnum.Admin & role) != 0;
    }
}
```

**Usage:**
```csharp
if (RoleHelper.CanEdit(user.Role))
{
    // User can edit
}
```

---

## Claims-Based System

### Claims Added During Login

Located in: `AIDungeonPromptsWeb/Extensions/HttpContextExtensions.cs:18-23`

```csharp
var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimValueConstants.CanEdit, RoleHelper.CanEdit(user.Role).ToString()),
    new(ClaimValueConstants.IsAdmin, RoleHelper.IsAdmin(user.Role).ToString())
};
```

### Claim Constants

Located in: `AIDungeonPromptsWeb/Constants/ClaimValueConstants.cs`

```csharp
public static class ClaimValueConstants
{
    public const string CanEdit = "CanEdit";
    public const string IsAdmin = "IsAdmin";
}
```

---

## Security Best Practices

### 1. Return NotFound() Instead of Forbidden()

**Why:** Prevents information disclosure about resource existence

```csharp
// ✅ GOOD - Doesn't reveal if resource exists
if (prompt == null || !userCanAccess)
{
    return NotFound();
}

// ❌ BAD - Reveals resource exists but user lacks access
if (prompt == null)
{
    return NotFound();
}
if (!userCanAccess)
{
    return Forbid(); // Leaks information!
}
```

### 2. Always Check Both Null and Authorization

```csharp
// ✅ GOOD - Single check for both existence and authorization
if (prompt == null || !IsAuthorized(prompt, user))
{
    return NotFound();
}

// ❌ BAD - Two separate checks
if (prompt == null) return NotFound();
if (!IsAuthorized(prompt, user)) return Forbid();
```

### 3. Use Policy Attributes for Controllers/Actions

```csharp
// ✅ GOOD - Declarative authorization
[Authorize(Policy = PolicyValueConstants.AdminsOnly)]
public class AdminController : Controller { }

// ❌ BAD - Manual checks everywhere
public class AdminController : Controller
{
    public IActionResult Index()
    {
        if (!User.IsInRole("Admin")) return Forbid();
        // ... rest of action
    }
}
```

### 4. Combine Role and Ownership Checks

```csharp
// ✅ GOOD - Owner OR privileged user can perform action
bool canDelete = prompt.OwnerId == user.Id || (user.Role & RoleEnum.Delete) != 0;

// ❌ BAD - Only owner can delete (admins can't help users)
bool canDelete = prompt.OwnerId == user.Id;
```

---

## Testing Authorization Logic

### Test Cases for Prompt Deletion

Located in: `AIDungeonPrompts.Test/Controllers/PromptsControllerTests.cs` (if exists)

| Scenario | Owner? | Has Delete Role? | Expected Result |
|----------|--------|------------------|-----------------|
| User owns prompt | ✅ Yes | ❌ No | ✅ Can delete |
| User doesn't own | ❌ No | ✅ Yes (Admin/Moderator) | ✅ Can delete |
| User doesn't own | ❌ No | ❌ No | ❌ NotFound (403) |
| Prompt doesn't exist | N/A | N/A | ❌ NotFound (404) |
| User not authenticated | N/A | N/A | ❌ Redirect to login |

### Test Cases for Edit Permission

| Scenario | Owner? | Has Edit Claim? | Expected Result |
|----------|--------|-----------------|-----------------|
| User owns prompt | ✅ Yes | ✅ Yes | ✅ Can edit |
| User owns prompt | ✅ Yes | ❌ No | ✅ Can edit (owner override) |
| User doesn't own | ❌ No | ✅ Yes | ✅ Can edit (editor role) |
| User doesn't own | ❌ No | ❌ No | ❌ NotFound |

### Test Cases for Admin Panel Access

| Scenario | Has Admin Claim? | Expected Result |
|----------|------------------|-----------------|
| User is admin | ✅ Yes | ✅ Can access |
| User is not admin | ❌ No | ❌ Forbidden (403) |
| Not authenticated | N/A | ❌ Redirect to login |

---

## Common Authorization Pitfalls

### ❌ Pitfall 1: Only Checking on Client Side

**Problem:**
```html
@if (RoleHelper.IsAdmin(user.Role))
{
    <a asp-action="Delete" asp-route-id="@Model.Id">Delete</a>
}
<!-- If user modifies HTML, they could still call Delete action -->
```

**Solution:**
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    // ALWAYS verify on server side
    if (!_currentUserService.TryGetCurrentUser(out var user))
    {
        return Unauthorized();
    }
    
    var prompt = await _mediator.Send(new GetPromptQuery(id));
    if (prompt == null || !CanDelete(prompt, user))
    {
        return NotFound();
    }
    
    // Proceed with deletion
}
```

### ❌ Pitfall 2: Incorrect Bitwise Operations

**Problem:**
```csharp
// Wrong: Uses OR instead of AND
if ((user.Role | RoleEnum.Delete) != 0) // Always true if Delete is non-zero!
{
    // Execute delete
}
```

**Solution:**
```csharp
// Correct: Uses AND to check if flag is set
if ((user.Role & RoleEnum.Delete) != 0)
{
    // Execute delete
}
```

### ❌ Pitfall 3: Exposing Different Error Messages

**Problem:**
```csharp
if (prompt == null)
{
    return NotFound("Prompt not found");
}
if (prompt.OwnerId != user.Id)
{
    return Forbid("You don't own this prompt");
}
// Attacker learns prompt exists but they don't own it
```

**Solution:**
```csharp
if (prompt == null || prompt.OwnerId != user.Id)
{
    return NotFound(); // Same response for both cases
}
```

---

## Admin Capabilities

### AdminController Actions

Located in: `AIDungeonPromptsWeb/Controllers/AdminController.cs`

All actions require `AdminsOnly` policy:

1. **Unlock Accounts**
   ```csharp
   [HttpPost("[controller]/unlock/{userId}")]
   [ValidateAntiForgeryToken]
   public async Task<IActionResult> UnlockAccount(int userId)
   ```

2. **View All Users**
   ```csharp
   [HttpGet("[controller]")]
   public async Task<IActionResult> Index()
   ```

3. **Change User Roles** (future implementation)
4. **Reset Passwords** (future implementation)

---

## Role Assignment

### First User Auto-Admin

Located in: `AIDungeonPrompts.Application/Commands/CreateUser/CreateUserCommandHandler.cs:32-36`

```csharp
// Check if this is the first user in the system
bool isFirstUser = !await _dbContext.Users.AnyAsync(cancellationToken);

var user = new User
{
    Role = isFirstUser ? RoleEnum.Admin : RoleEnum.None // First user is automatically admin
};
```

### Subsequent Role Changes

Must be done by admin through:
1. Admin panel (web interface) - **Recommended**
2. Direct database update - **For emergencies only**

```sql
-- Grant all permissions to user ID 2
UPDATE "Users" 
SET "Role" = 15  -- 15 = Delete(1) + Edit(2) + Create(4) + Admin(8)
WHERE "Id" = 2;
```

---

## Monitoring Authorization

### Logging Failed Authorization Attempts

**Recommendation:** Add logging for security auditing

```csharp
public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
{
    if (!_currentUserService.TryGetCurrentUser(out GetUserViewModel? user))
    {
        _logger.LogWarning("Unauthorized delete attempt on prompt {PromptId}", id);
        return NotFound();
    }
    
    var prompt = await _mediator.Send(new GetPromptQuery(id.Value), cancellationToken);
    
    if (prompt == null || !CanDelete(prompt, user))
    {
        _logger.LogWarning("User {UserId} attempted to delete prompt {PromptId} without permission", 
            user.Id, id);
        return NotFound();
    }
    
    // Proceed
}
```

---

## Future Enhancements

### Planned Improvements

1. **Granular Permissions**
   - Separate "Delete Own" vs "Delete Any" permissions
   - Resource-level permissions (e.g., specific prompt access)

2. **Role Management UI**
   - Admin interface for assigning roles
   - Bulk role operations
   - Role audit logs

3. **Permission Groups**
   - Pre-defined role templates (e.g., "Moderator", "ContentCreator")
   - Custom permission groups

4. **Time-Based Permissions**
   - Temporary elevated access
   - Scheduled permission changes

---

## Quick Reference

### Check if User Can Edit

```csharp
bool canEdit = RoleHelper.CanEdit(user.Role);
```

### Check if User Is Admin

```csharp
bool isAdmin = RoleHelper.IsAdmin(user.Role);
```

### Check Ownership or Elevated Permission

```csharp
bool canDelete = resource.OwnerId == user.Id || (user.Role & RoleEnum.Delete) != 0;
```

### Secure Authorization Pattern

```csharp
// 1. Get user
if (!_currentUserService.TryGetCurrentUser(out var user))
{
    return Unauthorized(); // Or redirect to login
}

// 2. Get resource
var resource = await GetResource(id);

// 3. Check both existence and authorization in one condition
if (resource == null || !IsAuthorized(resource, user))
{
    return NotFound(); // Doesn't leak resource existence
}

// 4. Proceed with action
```

---

**Last Updated:** October 2025  
**Version:** 2.1.0
