# Feedback System + Admin Panel — Implementation Plan

## Overview

A floating feedback button visible on every page, which opens a modal form. Feedback is stored in PostgreSQL. Admins can log in and view/manage all feedback via a protected admin panel.

---

## Tech Stack Alignment

- **Framework:** ASP.NET Core 9.0 (existing)
- **Database:** PostgreSQL via EF Core (existing)
- **Auth:** ASP.NET Identity with Roles (extend existing)
- **API auth:** JWT with `admin` role claim (extend existing)
- **UI:** Bootstrap 5.3.3 modal + Razor views (existing)

---

## Architecture

```
User clicks feedback button → Modal opens → Form submits (POST /Feedback/Submit)
                                                        ↓
                                              FeedbackController
                                                        ↓
                                               Feedback saved to DB
                                                        ↓
Admin visits /Admin/Index → Authenticated as Admin role → Views feedback list
                                                        ↓
                                        Can update status / add notes
```

---

## Part 1 — Database: Feedback Model + Migration

### 1.1 Feedback.cs (new model)

**File:** `/Models/Feedback.cs`

```csharp
namespace dupi.Models;

public enum FeedbackCategory
{
    General,
    Bug,
    FeatureRequest,
    Other
}

public enum FeedbackStatus
{
    New,
    InProgress,
    Resolved,
    Dismissed
}

public class Feedback
{
    public int Id { get; set; }
    public string? UserId { get; set; }          // null if anonymous
    public string? UserEmail { get; set; }        // null if anonymous
    public FeedbackCategory Category { get; set; } = FeedbackCategory.General;
    public string Message { get; set; } = string.Empty;
    public int? Rating { get; set; }             // 1–5 stars, optional
    public string? PageUrl { get; set; }          // where they submitted from
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;
    public string? AdminNotes { get; set; }
}
```

### 1.2 Add to ApplicationDbContext

**File:** `/Data/ApplicationDbContext.cs`

Add inside the class:
```csharp
public DbSet<Feedback> Feedbacks { get; set; }
```

### 1.3 EF Core Migration

Run:
```bash
dotnet ef migrations add AddFeedbackTable
dotnet ef database update
```

---

## Part 2 — Admin Role Setup

### 2.1 Extend ApplicationUser (if not already done)

**File:** `/Data/ApplicationUser.cs`

No changes needed — ASP.NET Identity already supports roles via `IdentityRole`.

### 2.2 Seed Admin Role + Admin User

**File:** `/Data/DbSeeder.cs` (new file)

```csharp
using Microsoft.AspNetCore.Identity;
using dupi.Data;

namespace dupi.Data;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        // Create Admin role
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        // Read admin credentials from config/env
        var adminEmail = config["AdminSeed:Email"] ?? "admin@dupi.app";
        var adminPassword = config["AdminSeed:Password"] ?? "Admin@123456";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
        else if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
        {
            await userManager.AddToRoleAsync(existingAdmin, "Admin");
        }
    }
}
```

### 2.3 Call seeder in Program.cs

**File:** `/Program.cs`

After `app.UseAuthorization();` and before `app.Run();`:

```csharp
// Seed admin role + user on startup
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAdminAsync(scope.ServiceProvider);
}
```

Also ensure Identity uses roles — in the Identity registration section add:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => { ... })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
```

(Change `AddDefaultIdentity` → `AddIdentity` so roles are included)

### 2.4 appsettings.json (or environment variables)

```json
"AdminSeed": {
  "Email": "admin@dupi.app",
  "Password": "CHANGE_THIS_IN_PRODUCTION"
}
```

> **Production:** Set `AdminSeed__Email` and `AdminSeed__Password` as environment variables. Never commit real passwords.

---

## Part 3 — Feedback Service

**File:** `/Services/FeedbackService.cs`

```csharp
using dupi.Data;
using dupi.Models;
using Microsoft.EntityFrameworkCore;

namespace dupi.Services;

public class FeedbackService
{
    private readonly ApplicationDbContext _db;

    public FeedbackService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SubmitAsync(Feedback feedback)
    {
        feedback.CreatedAt = DateTime.UtcNow;
        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Feedback>> GetAllAsync(FeedbackStatus? statusFilter = null)
    {
        var query = _db.Feedbacks.AsQueryable();
        if (statusFilter.HasValue)
            query = query.Where(f => f.Status == statusFilter.Value);
        return await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
    }

    public async Task<Feedback?> GetByIdAsync(int id)
        => await _db.Feedbacks.FindAsync(id);

    public async Task UpdateStatusAsync(int id, FeedbackStatus status, string? adminNotes)
    {
        var feedback = await _db.Feedbacks.FindAsync(id);
        if (feedback == null) return;
        feedback.Status = status;
        if (adminNotes != null) feedback.AdminNotes = adminNotes;
        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<FeedbackStatus, int>> GetCountsByStatusAsync()
    {
        return await _db.Feedbacks
            .GroupBy(f => f.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }
}
```

Register in Program.cs:
```csharp
builder.Services.AddScoped<FeedbackService>();
```

---

## Part 4 — Feedback Web Controller

**File:** `/Controllers/FeedbackController.cs`

```csharp
using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Controllers;

public class FeedbackController : Controller
{
    private readonly FeedbackService _feedbackService;

    public FeedbackController(FeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        string message,
        FeedbackCategory category,
        int? rating,
        string? pageUrl)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Length > 2000)
            return BadRequest("Invalid message.");

        var feedback = new Feedback
        {
            Message = message.Trim(),
            Category = category,
            Rating = rating is >= 1 and <= 5 ? rating : null,
            PageUrl = pageUrl,
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserEmail = User.FindFirstValue(ClaimTypes.Email)
        };

        await _feedbackService.SubmitAsync(feedback);

        return Ok(new { success = true, message = "Thank you for your feedback!" });
    }
}
```

---

## Part 5 — Admin Controller

**File:** `/Controllers/AdminController.cs`

```csharp
using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dupi.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly FeedbackService _feedbackService;

    public AdminController(FeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    public async Task<IActionResult> Index(FeedbackStatus? status)
    {
        var feedbacks = await _feedbackService.GetAllAsync(status);
        var counts = await _feedbackService.GetCountsByStatusAsync();

        ViewBag.StatusFilter = status;
        ViewBag.Counts = counts;
        return View(feedbacks);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var feedback = await _feedbackService.GetByIdAsync(id);
        if (feedback == null) return NotFound();
        return View(feedback);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, FeedbackStatus status, string? adminNotes)
    {
        await _feedbackService.UpdateStatusAsync(id, status, adminNotes);
        return RedirectToAction(nameof(Detail), new { id });
    }
}
```

---

## Part 6 — API Endpoints (for mobile app)

**File:** `/Api/FeedbackApiController.cs`

```csharp
using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Api;

[ApiController]
[Route("api/feedback")]
public class FeedbackApiController : ControllerBase
{
    private readonly FeedbackService _feedbackService;

    public FeedbackApiController(FeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    // Any user (authenticated or anonymous) can submit
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Length > 2000)
            return BadRequest("Message is required and must be under 2000 characters.");

        var feedback = new Feedback
        {
            Message = dto.Message.Trim(),
            Category = dto.Category,
            Rating = dto.Rating is >= 1 and <= 5 ? dto.Rating : null,
            PageUrl = dto.PageUrl,
            UserId = User.FindFirstValue("dupi:uid"),
            UserEmail = User.FindFirstValue(ClaimTypes.Email)
        };

        await _feedbackService.SubmitAsync(feedback);
        return Ok(new { success = true });
    }

    // Admin-only: list all feedback
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] FeedbackStatus? status)
    {
        var list = await _feedbackService.GetAllAsync(status);
        return Ok(list);
    }

    // Admin-only: update feedback status
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        await _feedbackService.UpdateStatusAsync(id, dto.Status, dto.AdminNotes);
        return Ok(new { success = true });
    }
}

public record SubmitFeedbackDto(
    string Message,
    FeedbackCategory Category = FeedbackCategory.General,
    int? Rating = null,
    string? PageUrl = null
);

public record UpdateStatusDto(FeedbackStatus Status, string? AdminNotes);
```

### JWT Admin Role Claim

For JWT admin role to work, add role claim in `JwtTokenService.GenerateToken`:

```csharp
// Inside GenerateToken method, after existing claims:
var roles = await _userManager.GetRolesAsync(user);
foreach (var role in roles)
    claims.Add(new Claim(ClaimTypes.Role, role));
```

(Inject `UserManager<ApplicationUser>` into `JwtTokenService`)

---

## Part 7 — Razor Views

### 7.1 Feedback Modal (partial) — added to `_Layout.cshtml`

Add just before `</body>`:

```html
<!-- Feedback Button -->
@if (User.Identity?.IsAuthenticated == true || true) <!-- show to all users -->
{
<button id="feedbackBtn"
        class="btn btn-primary rounded-circle shadow"
        style="position:fixed;bottom:24px;right:24px;width:52px;height:52px;z-index:1050;"
        data-bs-toggle="modal" data-bs-target="#feedbackModal"
        title="Send Feedback">
    <i class="bi bi-chat-square-text-fill"></i>
</button>

<!-- Feedback Modal -->
<div class="modal fade" id="feedbackModal" tabindex="-1" aria-labelledby="feedbackModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow-lg">
            <div class="modal-header border-0 pb-0">
                <h5 class="modal-title fw-semibold" id="feedbackModalLabel">
                    <i class="bi bi-chat-square-heart me-2 text-primary"></i>Share Feedback
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body pt-2">
                <div id="feedbackSuccess" class="alert alert-success d-none" role="alert">
                    <i class="bi bi-check-circle me-2"></i>Thank you! Your feedback has been received.
                </div>
                <form id="feedbackForm">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="pageUrl" id="feedbackPageUrl" />

                    <div class="mb-3">
                        <label class="form-label small fw-medium">Category</label>
                        <div class="d-flex gap-2 flex-wrap">
                            <input type="radio" class="btn-check" name="category" id="cat-general" value="0" checked>
                            <label class="btn btn-outline-secondary btn-sm" for="cat-general">General</label>

                            <input type="radio" class="btn-check" name="category" id="cat-bug" value="1">
                            <label class="btn btn-outline-danger btn-sm" for="cat-bug">
                                <i class="bi bi-bug me-1"></i>Bug
                            </label>

                            <input type="radio" class="btn-check" name="category" id="cat-feature" value="2">
                            <label class="btn btn-outline-success btn-sm" for="cat-feature">
                                <i class="bi bi-lightbulb me-1"></i>Feature
                            </label>

                            <input type="radio" class="btn-check" name="category" id="cat-other" value="3">
                            <label class="btn btn-outline-secondary btn-sm" for="cat-other">Other</label>
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="form-label small fw-medium">How would you rate your experience?</label>
                        <div class="d-flex gap-1" id="starRating">
                            @for (int i = 1; i <= 5; i++)
                            {
                                <button type="button" class="btn btn-link p-0 star-btn fs-4 text-muted" data-value="@i">
                                    <i class="bi bi-star"></i>
                                </button>
                            }
                        </div>
                        <input type="hidden" name="rating" id="ratingValue" value="" />
                    </div>

                    <div class="mb-3">
                        <label for="feedbackMessage" class="form-label small fw-medium">
                            Your feedback <span class="text-danger">*</span>
                        </label>
                        <textarea class="form-control"
                                  id="feedbackMessage"
                                  name="message"
                                  rows="4"
                                  maxlength="2000"
                                  placeholder="Tell us what you think, what's broken, or what you'd love to see..."
                                  required></textarea>
                        <div class="form-text text-end">
                            <span id="charCount">0</span>/2000
                        </div>
                    </div>

                    <button type="submit" class="btn btn-primary w-100" id="feedbackSubmitBtn">
                        <i class="bi bi-send me-2"></i>Send Feedback
                    </button>
                </form>
            </div>
        </div>
    </div>
</div>
}

<script>
    // Feedback modal logic
    (function () {
        const form = document.getElementById('feedbackForm');
        if (!form) return;

        // Set current page URL
        document.getElementById('feedbackPageUrl').value = window.location.pathname;

        // Character counter
        const msg = document.getElementById('feedbackMessage');
        const charCount = document.getElementById('charCount');
        msg.addEventListener('input', () => charCount.textContent = msg.value.length);

        // Star rating
        const stars = document.querySelectorAll('.star-btn');
        const ratingInput = document.getElementById('ratingValue');
        stars.forEach(btn => {
            btn.addEventListener('click', () => {
                const val = parseInt(btn.dataset.value);
                ratingInput.value = val;
                stars.forEach((s, i) => {
                    const icon = s.querySelector('i');
                    icon.className = i < val ? 'bi bi-star-fill text-warning' : 'bi bi-star text-muted';
                });
            });
        });

        // Form submission
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const btn = document.getElementById('feedbackSubmitBtn');
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Sending...';

            const data = new FormData(form);
            try {
                const resp = await fetch('/Feedback/Submit', { method: 'POST', body: data });
                if (resp.ok) {
                    form.classList.add('d-none');
                    document.getElementById('feedbackSuccess').classList.remove('d-none');
                    setTimeout(() => {
                        const modal = bootstrap.Modal.getInstance(document.getElementById('feedbackModal'));
                        modal?.hide();
                        form.reset();
                        form.classList.remove('d-none');
                        document.getElementById('feedbackSuccess').classList.add('d-none');
                        charCount.textContent = '0';
                        ratingInput.value = '';
                        stars.forEach(s => s.querySelector('i').className = 'bi bi-star text-muted');
                        btn.disabled = false;
                        btn.innerHTML = '<i class="bi bi-send me-2"></i>Send Feedback';
                    }, 2500);
                } else {
                    alert('Something went wrong. Please try again.');
                    btn.disabled = false;
                    btn.innerHTML = '<i class="bi bi-send me-2"></i>Send Feedback';
                }
            } catch {
                alert('Network error. Please try again.');
                btn.disabled = false;
                btn.innerHTML = '<i class="bi bi-send me-2"></i>Send Feedback';
            }
        });
    })();
</script>
```

### 7.2 Admin Feedback List — `/Views/Admin/Index.cshtml`

```cshtml
@model List<dupi.Models.Feedback>
@{
    ViewData["Title"] = "Admin — Feedback";
    var counts = (Dictionary<dupi.Models.FeedbackStatus, int>)ViewBag.Counts;
    var currentStatus = (dupi.Models.FeedbackStatus?)ViewBag.StatusFilter;
}

<div class="container-fluid py-4">
    <div class="d-flex align-items-center justify-content-between mb-4">
        <h2 class="fw-bold mb-0"><i class="bi bi-chat-square-text me-2 text-primary"></i>Feedback</h2>
        <span class="badge bg-primary fs-6">@Model.Count entries</span>
    </div>

    <!-- Status filter tabs -->
    <ul class="nav nav-pills mb-4">
        <li class="nav-item">
            <a class="nav-link @(currentStatus == null ? "active" : "")" href="/Admin">
                All <span class="badge bg-secondary ms-1">@counts.Values.Sum()</span>
            </a>
        </li>
        @foreach (var status in Enum.GetValues<dupi.Models.FeedbackStatus>())
        {
            var count = counts.GetValueOrDefault(status, 0);
            var badgeClass = status switch {
                dupi.Models.FeedbackStatus.New => "bg-danger",
                dupi.Models.FeedbackStatus.InProgress => "bg-warning text-dark",
                dupi.Models.FeedbackStatus.Resolved => "bg-success",
                _ => "bg-secondary"
            };
            <li class="nav-item">
                <a class="nav-link @(currentStatus == status ? "active" : "")"
                   href="/Admin?status=@((int)status)">
                    @status <span class="badge @badgeClass ms-1">@count</span>
                </a>
            </li>
        }
    </ul>

    @if (!Model.Any())
    {
        <div class="text-center py-5 text-muted">
            <i class="bi bi-inbox fs-1 d-block mb-3"></i>
            No feedback yet.
        </div>
    }
    else
    {
        <div class="table-responsive">
            <table class="table table-hover align-middle">
                <thead class="table-light">
                    <tr>
                        <th>#</th>
                        <th>Category</th>
                        <th>Message</th>
                        <th>Rating</th>
                        <th>User</th>
                        <th>Page</th>
                        <th>Date</th>
                        <th>Status</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var f in Model)
                    {
                        var statusBadge = f.Status switch {
                            dupi.Models.FeedbackStatus.New => "danger",
                            dupi.Models.FeedbackStatus.InProgress => "warning",
                            dupi.Models.FeedbackStatus.Resolved => "success",
                            _ => "secondary"
                        };
                        <tr>
                            <td class="text-muted small">@f.Id</td>
                            <td><span class="badge bg-light text-dark border">@f.Category</span></td>
                            <td style="max-width:300px">
                                <span class="text-truncate d-inline-block" style="max-width:280px">
                                    @f.Message
                                </span>
                            </td>
                            <td>
                                @if (f.Rating.HasValue)
                                {
                                    @for (int i = 1; i <= 5; i++)
                                    {
                                        <i class="bi bi-star-fill @(i <= f.Rating ? "text-warning" : "text-muted")" style="font-size:.75rem"></i>
                                    }
                                }
                                else
                                {
                                    <span class="text-muted">—</span>
                                }
                            </td>
                            <td class="small text-muted">@(f.UserEmail ?? "Anonymous")</td>
                            <td class="small text-muted text-truncate" style="max-width:120px">@(f.PageUrl ?? "—")</td>
                            <td class="small text-muted text-nowrap">@f.CreatedAt.ToString("MMM d, yyyy")</td>
                            <td><span class="badge bg-@statusBadge">@f.Status</span></td>
                            <td>
                                <a href="/Admin/Detail/@f.Id" class="btn btn-sm btn-outline-primary">View</a>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>
```

### 7.3 Admin Feedback Detail — `/Views/Admin/Detail.cshtml`

```cshtml
@model dupi.Models.Feedback
@{
    ViewData["Title"] = $"Feedback #{Model.Id}";
}

<div class="container py-4" style="max-width:720px">
    <a href="/Admin" class="btn btn-sm btn-outline-secondary mb-4">
        <i class="bi bi-arrow-left me-1"></i>Back to all
    </a>

    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body p-4">
            <div class="d-flex justify-content-between align-items-start mb-3">
                <div>
                    <span class="badge bg-light text-dark border me-2">@Model.Category</span>
                    @{
                        var statusBadge = Model.Status switch {
                            dupi.Models.FeedbackStatus.New => "danger",
                            dupi.Models.FeedbackStatus.InProgress => "warning",
                            dupi.Models.FeedbackStatus.Resolved => "success",
                            _ => "secondary"
                        };
                    }
                    <span class="badge bg-@statusBadge">@Model.Status</span>
                </div>
                <span class="text-muted small">@Model.CreatedAt.ToString("MMMM d, yyyy 'at' HH:mm") UTC</span>
            </div>

            @if (Model.Rating.HasValue)
            {
                <div class="mb-3">
                    @for (int i = 1; i <= 5; i++)
                    {
                        <i class="bi bi-star-fill @(i <= Model.Rating ? "text-warning" : "text-muted")"></i>
                    }
                </div>
            }

            <p class="fs-5 mb-4" style="white-space:pre-wrap">@Model.Message</p>

            <dl class="row small text-muted mb-0">
                <dt class="col-sm-3">From</dt>
                <dd class="col-sm-9">@(Model.UserEmail ?? "Anonymous")</dd>
                <dt class="col-sm-3">Page</dt>
                <dd class="col-sm-9">@(Model.PageUrl ?? "—")</dd>
            </dl>
        </div>
    </div>

    <!-- Admin actions -->
    <div class="card shadow-sm border-0">
        <div class="card-body p-4">
            <h6 class="fw-semibold mb-3">Admin Actions</h6>
            <form method="post" action="/Admin/UpdateStatus">
                @Html.AntiForgeryToken()
                <input type="hidden" name="id" value="@Model.Id" />

                <div class="mb-3">
                    <label class="form-label small fw-medium">Status</label>
                    <select name="status" class="form-select">
                        @foreach (var status in Enum.GetValues<dupi.Models.FeedbackStatus>())
                        {
                            <option value="@((int)status)" @(Model.Status == status ? "selected" : "")>@status</option>
                        }
                    </select>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-medium">Admin Notes</label>
                    <textarea name="adminNotes" class="form-control" rows="3"
                              placeholder="Internal notes...">@Model.AdminNotes</textarea>
                </div>

                <button type="submit" class="btn btn-primary">
                    <i class="bi bi-check2 me-1"></i>Save Changes
                </button>
            </form>
        </div>
    </div>
</div>
```

### 7.4 Add Admin link to sidebar (for admin users)

In `/Views/Shared/_Layout.cshtml`, inside the sidebar nav links, add:

```html
@if (User.IsInRole("Admin"))
{
    <li class="nav-item">
        <a class="nav-link sidebar-link d-flex align-items-center gap-2 @(ViewContext.RouteData.Values["controller"]?.ToString() == "Admin" ? "active" : "")"
           href="/Admin">
            <i class="bi bi-shield-check sidebar-icon"></i>
            <span class="sidebar-text">Admin</span>
        </a>
    </li>
}
```

---

## Part 8 — Program.cs Changes Summary

```csharp
// 1. Change AddDefaultIdentity to AddIdentity (to support roles)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 2. Register FeedbackService
builder.Services.AddScoped<FeedbackService>();

// 3. After app.UseAuthorization(), seed admin:
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAdminAsync(scope.ServiceProvider);
}
```

---

## Implementation Order (Step-by-Step)

1. **Create `/Models/Feedback.cs`**
2. **Add `DbSet<Feedback>` to ApplicationDbContext**
3. **Run `dotnet ef migrations add AddFeedbackTable && dotnet ef database update`**
4. **Create `/Data/DbSeeder.cs`**
5. **Create `/Services/FeedbackService.cs`**
6. **Update `Program.cs`** — switch to `AddIdentity`, register `FeedbackService`, call `DbSeeder`
7. **Create `/Controllers/FeedbackController.cs`**
8. **Create `/Controllers/AdminController.cs`**
9. **Create `/Api/FeedbackApiController.cs`**
10. **Add feedback button + modal to `_Layout.cshtml`**
11. **Create `/Views/Admin/` directory**
12. **Create `/Views/Admin/Index.cshtml`**
13. **Create `/Views/Admin/Detail.cshtml`**
14. **Add Admin sidebar link to `_Layout.cshtml`**
15. **Update `JwtTokenService` to include role claims**
16. **Set `AdminSeed:Email` and `AdminSeed:Password` in appsettings / env vars**
17. **Test: submit feedback → check DB → log in as admin → verify panel**

---

## Security Checklist

- [x] `[Authorize(Roles = "Admin")]` on all admin endpoints
- [x] `[ValidateAntiForgeryToken]` on all POST actions
- [x] Message length capped at 2000 chars
- [x] Admin password read from config (never hardcoded in prod)
- [x] JWT role claims added so mobile admin access also works
- [x] Anonymous feedback allowed but user ID/email captured if logged in

---

## Future Enhancements (not in scope now)

- Email notification to admin when new feedback arrives
- Pagination on admin feedback list
- Bulk status updates
- Feedback analytics dashboard (ratings over time, category breakdown)
- Allow admin to promote other users to admin role via UI
