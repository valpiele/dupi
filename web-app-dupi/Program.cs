using System.Text;
using dupi.Data;
using dupi.Hubs;
using dupi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Resolve connection string — try DATABASE_URL, then individual PG* vars, then appsettings
string connectionString;
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    connectionString = databaseUrl.StartsWith("postgresql://") || databaseUrl.StartsWith("postgres://")
        ? ConvertDatabaseUrl(databaseUrl)
        : databaseUrl;
}
else
{
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    if (!string.IsNullOrEmpty(pgHost))
    {
        var pgPort     = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
        var pgUser     = Environment.GetEnvironmentVariable("PGUSER") ?? "postgres";
        var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "";
        var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE") ?? "railway";
        connectionString = $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword};SSL Mode=Require;Trust Server Certificate=true";
    }
    else
    {
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No database connection string found.");
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

// JWT key — use configured key or generate a dev key
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? (builder.Environment.IsDevelopment() ? "dev-key-change-in-production-min-32-chars!" : null)
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "dupi";

// Dual authentication: cookies for web, JWT for API
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Allow JWT token from query string for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

// CORS for mobile app
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();
builder.Services.AddSignalR();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<NutritionService>();
builder.Services.AddScoped<SocialService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ChallengeService>();
builder.Services.AddScoped<FunFactService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpClient<GeminiService>(c => c.Timeout = TimeSpan.FromMinutes(5));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("MobileApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapHub<ChatHub>("/chatHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static string ConvertDatabaseUrl(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');
    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
