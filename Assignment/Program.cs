using Assignment.App_Start;
using Assignment.Middleware;
using Assignment.Models;
using Assignment.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Web.config holds the connection string, as in classic ASP.NET (see README).
builder.Configuration.AddXmlFile("Web.config", optional: true, reloadOnChange: false);

builder.Services.AddControllersWithViews(FilterConfig.RegisterGlobalFilters);

string provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
builder.Services.AddDbContext<HotelManagementContext>(options =>
{
    if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        options.UseInMemoryDatabase(builder.Configuration["Database:Name"] ?? "HotelManagement");
    else
        options.UseSqlServer(ConnectionStrings.Resolve(builder.Configuration));
});

builder.Services
    .AddIdentity<AppUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 8;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<HotelManagementContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.SlidingExpiration = true;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection("Billing"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<ReservationService>();
if (builder.Configuration.GetValue("Workflow:EnableAutomation", true))
    builder.Services.AddHostedService<ReservationAutomationService>();

WebApplication app = builder.Build();

if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HotelManagementContext>();
    if (db.Database.IsRelational())
    {
        if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
            await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
    await DataSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSecurityHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
// Scripts/ and Content/ follow the classic ASP.NET MVC layout.
foreach (string folder in new[] { "Scripts", "Content" })
{
    string path = Path.Combine(app.Environment.ContentRootPath, folder);
    if (Directory.Exists(path))
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(path), RequestPath = "/" + folder });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

RouteConfig.RegisterRoutes(app);

app.Run();

/// <summary>Lets integration tests start the app with WebApplicationFactory.</summary>
public partial class Program { }

/// <summary>Picks the connection string: Web.config's HotelDBContext if it has been filled in, otherwise appsettings.json's DefaultConnection.</summary>
public static class ConnectionStrings
{
    public const string WebConfigKey = "connectionStrings:add:HotelDBContext:connectionString";

    public static string Resolve(IConfiguration config)
    {
        string? fromWebConfig = config[WebConfigKey];
        if (!string.IsNullOrWhiteSpace(fromWebConfig) && !fromWebConfig.Contains("YOUR_SERVER", StringComparison.OrdinalIgnoreCase))
            return fromWebConfig;
        return config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No connection string: set HotelDBContext in Web.config or DefaultConnection in appsettings.json.");
    }
}
