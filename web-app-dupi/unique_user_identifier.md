# Unique User Identifier (Username) — Implementation Plan

## 1. Current State & Problems

### What exists today
- `ApplicationUser` is empty — inherits from `IdentityUser` with no extra fields
- `IdentityUser.UserName` is set to the user's **email** on registration (the "malformed" identifier)
- A `Username` field lives inside `UserProfile` — a JSON blob in Azure Blob Storage, NOT in PostgreSQL
- `ProfileService.IsUsernameTaken()` checks uniqueness by **iterating every blob** in the container — O(n), slow, and not atomic
- Current validation regex: `^[a-zA-Z0-9_-]+$` (allows hyphens, disallows periods — wrong)
- Auto-generated suggestion uses **hyphens** (`SuggestUsername`) which the new rules will reject
- `FriendDto`, `ConversationDto`, `DiscoverProfileDto` all have a `Username` field populated from blob — not guaranteed unique

### The race condition
Between `IsUsernameTaken()` returning `false` and `SaveProfile()` writing to blob, two concurrent users can both claim the same username. The blob storage has no atomic compare-and-swap for this. This is a **classic TOCTOU (Time-Of-Check-Time-Of-Use)** race.

---

## 2. Instagram Username Rules

| Rule | Detail |
|---|---|
| Length | 1–30 characters |
| Allowed chars | `a-z`, `A-Z`, `0-9`, `.` (period), `_` (underscore) |
| No other chars | no hyphens, no spaces, no `@` |
| Period restrictions | Cannot **start** with `.`, cannot **end** with `.`, no **consecutive** periods (`..`) |
| Not all numbers | At least one non-numeric character |
| Case insensitive | `JohnDoe` and `johndoe` are the same username |
| Uniqueness | Globally unique across the platform |

Regex (server-side validation):
```
^(?!.*\.\.)(?!\.)[a-zA-Z0-9._]{1,30}(?<!\.)$
```
Plus a secondary check: `!Regex.IsMatch(username, @"^\d+$")` to reject all-numeric.

---

## 3. Chosen Architecture — Move Username to PostgreSQL

### Why not stay in Blob Storage?
- Blob has no UNIQUE constraint, no transactions, no atomic compare-and-swap
- `IsUsernameTaken()` is O(n) and races
- A distributed lock (Redis, Postgres advisory lock) on top of blob is unnecessary complexity

### The right solution: `DupiUsername` column on `ApplicationUser` + PostgreSQL UNIQUE INDEX
- PostgreSQL's UNIQUE constraint is **atomic** — two concurrent `INSERT`/`UPDATE` statements for the same value block each other; exactly one succeeds and the other gets a `23505` unique violation
- No distributed lock needed — the DB does this for free
- Lookup by username becomes O(log n) with an index instead of O(n) blob scan
- EF Core catches `DbUpdateException` with `IsUniqueViolation()` for clean error handling

### Why not reuse `IdentityUser.UserName`?
Identity uses `UserName` internally for authentication lookups and normalizes it. Changing its meaning from email to Instagram-style username would break `FindByEmailAsync`, `CheckPasswordSignInAsync`, and Identity's default login flow. Better to add a **separate column** `DupiUsername`.

---

## 4. Database Changes

### 4a. Extend `ApplicationUser`
**File:** `Data/ApplicationUser.cs`

```csharp
public class ApplicationUser : IdentityUser
{
    // Instagram-style username — globally unique, lowercase-normalized
    public string? DupiUsername { get; set; }
}
```

Nullable initially so the migration doesn't break existing rows.

### 4b. Unique Index in `ApplicationDbContext`
**File:** `Data/ApplicationDbContext.cs`

In `OnModelCreating`:
```csharp
builder.Entity<ApplicationUser>(e =>
{
    e.Property(u => u.DupiUsername).HasMaxLength(30);
    // Case-insensitive unique index using PostgreSQL citext or collation
    e.HasIndex(u => u.DupiUsername)
     .IsUnique()
     .HasFilter("\"DupiUsername\" IS NOT NULL");
});
```

Using a partial unique index (`WHERE DupiUsername IS NOT NULL`) allows the nullable migration phase without conflicts on NULL rows.

For true case-insensitive uniqueness, store `DupiUsername` **always lowercased** at write time (normalize in the service layer before saving — simpler than a PostgreSQL `citext` extension dependency).

### 4c. EF Core Migration

```bash
dotnet ef migrations add AddDupiUsername
dotnet ef database update
```

---

## 5. New `UsernameService`

**New file:** `Services/UsernameService.cs`

Responsibilities:
- Validate against Instagram rules
- Suggest a unique username during registration
- Check uniqueness via DB (not blob scan)
- Reserve/commit a username atomically via `DbUpdateException` catch
- Look up a user by username

```csharp
public class UsernameService
{
    private readonly UserManager<ApplicationUser> _userManager;

    // Validates Instagram rules — returns null if valid, error message if invalid
    public static string? Validate(string username) { ... }

    // Normalize: lowercase only
    public static string Normalize(string username) => username.ToLowerInvariant();

    // Generate a suggestion from email prefix, sanitized to allowed chars
    public string SuggestFromEmail(string email) { ... }

    // Try to set username; returns true on success, false on conflict
    public async Task<bool> TrySetUsernameAsync(ApplicationUser user, string username) { ... }

    // Find user by username (case-insensitive via normalized lowercase)
    public async Task<ApplicationUser?> FindByUsernameAsync(string username) { ... }
}
```

`TrySetUsernameAsync` implementation:
```csharp
public async Task<bool> TrySetUsernameAsync(ApplicationUser user, string username)
{
    var normalized = Normalize(username);
    user.DupiUsername = normalized;
    try
    {
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
    {
        return false; // someone else took it concurrently
    }
}
```

No locking, no retries — just let the DB constraint do its job. Non-blocking on the server: the `await` yields the thread; the DB round-trip is async; no spinloops or Mutex.

---

## 6. Registration Flow Changes

**File:** `Api/AuthApiController.cs`

On `POST /api/auth/register`:
1. Create `ApplicationUser` (email/password — unchanged)
2. Auto-generate `DupiUsername` from email prefix:
   - Strip domain, keep only `[a-z0-9._]`, trim leading/trailing periods, collapse `..` → `.`
   - If result is empty or all-numeric, prepend `user`
   - If still taken (conflict), append `_{random 4 digits}` and retry (max 5 attempts — extremely rare)
3. Return `DupiUsername` in `AuthResponse`

On `POST /api/auth/google`:
- Same logic using `payload.Name` or email prefix as the base suggestion

**Do NOT ask the user to pick a username at registration** — auto-generate it. They can change it in Profile settings later.

### Updated `AuthResponse`
```csharp
public class AuthResponse
{
    public string Token { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Username { get; set; }  // ← NEW: the @username
}
```

---

## 7. JWT Token

**File:** `Services/JwtTokenService.cs`

Add `dupi:username` claim alongside the existing `dupi:uid`:
```csharp
new Claim("dupi:username", dupiUsername ?? "")
```

This lets the frontend/mobile app read the username from the token without an extra API call.

---

## 8. Profile Update — Username Change

**File:** `Api/ProfileApiController.cs` (or wherever `PUT /api/profile` lives)

```csharp
[HttpPut("profile")]
public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
{
    // 1. Validate username format
    var error = UsernameService.Validate(req.Username);
    if (error != null) return BadRequest(new { error });

    // 2. Normalize
    var normalized = UsernameService.Normalize(req.Username);

    // 3. If unchanged, skip username update
    if (user.DupiUsername != normalized)
    {
        var success = await _usernameService.TrySetUsernameAsync(user, normalized);
        if (!success) return Conflict(new { error = "Username already taken." });
    }

    // 4. Update blob profile (displayName, bio, isPublic — not username)
    ...
}
```

The blob profile `UserProfile.Username` can be kept in sync as a denormalized cache (read from DB on profile load, written to blob as part of the save) — or the blob can drop the Username field entirely and always read it from the DB. The latter is cleaner.

---

## 9. Data Migration for Existing Users

Because `DupiUsername` starts as nullable, existing users have `NULL`. A **background migration script** (or a startup `IHostedService` that runs once) should:

1. Load all `ApplicationUser` rows where `DupiUsername IS NULL`
2. For each, load their blob profile's `Username`
3. Sanitize to new Instagram rules (strip hyphens → underscores, etc.)
4. Attempt `TrySetUsernameAsync` — if conflict, append suffix
5. Save

This runs async on startup (or as a one-shot migration endpoint), does not block requests, and is idempotent.

---

## 10. Lookup by Username

**New endpoint:** `GET /api/users/{username}` in `ProfileApiController`

```csharp
[HttpGet("users/{username}")]
public async Task<IActionResult> GetByUsername(string username)
{
    var normalized = UsernameService.Normalize(username);
    var user = await _usernameService.FindByUsernameAsync(normalized);
    if (user == null) return NotFound();
    // Load full profile from blob, return ProfileDto
}
```

Also update `POST /api/friends/request` to accept `@username` (or keep accepting `userId` — both should work).

---

## 11. Friends Section Changes

### DTOs (already correct structure)
`FriendDto` and `ConversationDto` already have a `Username` field. The only change is **where it comes from**: instead of `profile.Username` from blob, populate from `applicationUser.DupiUsername` (DB).

### `SocialService.cs`
All friend list queries use `ProfileService.GetProfileByUserId(friendId)` to get the username — a blob read per friend.

**Better approach:** Join `ApplicationUser` (which now has `DupiUsername`) directly in the EF query:

```csharp
var list = await _db.Friendships
    .Include(f => f.Sender)   // if navigation props are added
    .Include(f => f.Receiver)
    .Where(f => f.Status == FriendshipStatus.Accepted && ...)
    .ToListAsync();
```

Or alternatively, load usernames in a single `WHERE Id IN (...)` query via `UserManager`:
```csharp
var friendIds = list.Select(f => f.SenderId == userId ? f.ReceiverId : f.SenderId).ToList();
var users = _userManager.Users.Where(u => friendIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DupiUsername })
            .ToList();
```

This replaces N blob reads with 1 DB query.

### Views — Friends UI
**File:** `Views/Friends/` (Razor templates)

Show `@username` everywhere a user is referenced:
- Friend cards: show `@{friend.Username}` prominently, `DisplayName` as secondary
- Pending requests: show `@username` of sender
- Search/discover: `@username` as primary identifier

### Discover endpoint
`GET /api/discover` returns `DiscoverProfileDto` — populate `Username` from DB, not blob.

---

## 12. Summary of Files to Change

| File | Change |
|---|---|
| `Data/ApplicationUser.cs` | Add `DupiUsername` property |
| `Data/ApplicationDbContext.cs` | Add unique index on `DupiUsername` |
| `Services/UsernameService.cs` | **NEW** — validation, normalization, conflict-safe set, lookup |
| `Services/ProfileService.cs` | Remove `IsUsernameTaken()` (replaced by DB), update `IsValidUsername()` to Instagram rules, stop generating username suggestion (moved to `UsernameService`) |
| `Services/SocialService.cs` | Replace per-friend blob reads with a single DB query for usernames |
| `Api/AuthApiController.cs` | Auto-generate `DupiUsername` on registration; include in `AuthResponse` |
| `Api/ProfileApiController.cs` | Update `PUT /api/profile` to go through `UsernameService`; add `GET /api/users/{username}` |
| `Services/JwtTokenService.cs` | Add `dupi:username` claim |
| `Dtos/AuthDtos.cs` | Add `Username` to `AuthResponse` |
| `Dtos/SocialDtos.cs` | No struct change needed; source of `Username` changes |
| `Views/Friends/` | Show `@username` as primary identifier |
| `Views/Discover/` | Same |
| Migrations | `AddDupiUsername` migration |

---

## 13. Concurrency & Non-Blocking Guarantees

| Concern | Solution |
|---|---|
| Two users grab same username simultaneously | PostgreSQL `UNIQUE` constraint — DB serializes concurrent writes; second writer gets `23505` error |
| Server thread blocking | All operations are `async/await` — thread yields during DB I/O |
| Distributed lock | **Not needed** — DB constraint is the lock |
| Retry on conflict at registration | Max 5 attempts with different suffixes; each attempt is an async DB call |
| Username update conflict | Single `TrySetUsernameAsync` call — instant conflict detection, no polling |

---

## 14. Order of Implementation

1. `ApplicationUser.cs` + `ApplicationDbContext.cs` + migration → deploy (nullable, no breaking change)
2. `UsernameService.cs` — all logic, fully testable
3. `AuthApiController.cs` — auto-assign username on register
4. `JwtTokenService.cs` — add claim
5. `ProfileApiController.cs` — update + lookup endpoints
6. `SocialService.cs` — DB-joined friend username queries
7. Data migration for existing users (startup service)
8. Views — `@username` display
9. Remove `IsUsernameTaken` from `ProfileService`, update `IsValidUsername` regex
