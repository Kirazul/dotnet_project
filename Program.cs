using InvestPortfolio.Components;
using InvestPortfolio.Data;
using InvestPortfolio.Models;
using InvestPortfolio.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Radzen;
using System.Data;
using System.Globalization;

var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
culture.NumberFormat.CurrencySymbol = "$";
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ===== EF Core + SQLite =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// ===== Identity (TP11) =====
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configurer le cookie pour rediriger vers /login si non authentifié
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/api/auth/logout";
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddRadzenComponents();

// ===== Service d'arrière-plan : simulation des prix toutes les minutes =====
builder.Services.AddHostedService<PriceSimulationHostedService>();

var app = builder.Build();

// ===== Seeding : Admin + Budget =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    context.Database.EnsureCreated();
    EnsureUserOwnershipSchema(context);

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    var adminUser = await userManager.FindByEmailAsync("admin@invest.com");
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = "admin@invest.com", Email = "admin@invest.com" };
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (!result.Succeeded)
            adminUser = null;
    }

    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    if (adminUser != null)
    {
        AssignLegacyPortfolioData(context, adminUser.Id);

        if (!context.Budgets.Any(b => b.UserId == adminUser.Id))
        {
            context.Budgets.Add(new Budget
            {
                UserId = adminUser.Id,
                InitialAmount = 0,
                CurrentBalance = 0,
                CreatedAt = DateTime.Now
            });
            context.SaveChanges();
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// --- ENDPOINTS AUTH (TP11 - Minimal API) ---

app.MapPost("/api/auth/login", async (
    [FromServices] SignInManager<IdentityUser> signInManager,
    [FromForm] string email,
    [FromForm] string password) =>
{
    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
    if (result.Succeeded) return Results.Redirect("/dashboard");
    return Results.Redirect("/login?error=Identifiants incorrects");
}).DisableAntiforgery();

app.MapPost("/api/auth/register", async (
    [FromServices] UserManager<IdentityUser> userManager,
    [FromServices] SignInManager<IdentityUser> signInManager,
    [FromForm] string email,
    [FromForm] string password) =>
{
    var user = new IdentityUser { UserName = email, Email = email };
    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
    {
        await signInManager.SignInAsync(user, isPersistent: true);
        return Results.Redirect("/dashboard");
    }
    return Results.Redirect("/register?error=Erreur lors de la creation");
}).DisableAntiforgery();

app.MapPost("/api/auth/logout", async ([FromServices] SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.Run();

static void EnsureUserOwnershipSchema(AppDbContext context)
{
    EnsureColumn(context, "Assets", "UserId", "TEXT NOT NULL DEFAULT ''");
    EnsureColumn(context, "Budgets", "UserId", "TEXT NOT NULL DEFAULT ''");
    EnsureColumn(context, "Transactions", "UserId", "TEXT NOT NULL DEFAULT ''");
    EnsureColumn(context, "Tags", "UserId", "TEXT NOT NULL DEFAULT ''");

    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Assets_UserId ON Assets (UserId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Budgets_UserId ON Budgets (UserId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Transactions_UserId ON Transactions (UserId)");
    context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Tags_UserId ON Tags (UserId)");
}

static void EnsureColumn(AppDbContext context, string table, string column, string definition)
{
    var connection = context.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
    {
        connection.Open();
    }

    using var checkCommand = connection.CreateCommand();
    checkCommand.CommandText = $"PRAGMA table_info({table})";

    var exists = false;
    using (var reader = checkCommand.ExecuteReader())
    {
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }
    }

    if (!exists)
    {
        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
        alterCommand.ExecuteNonQuery();
    }
}

static void AssignLegacyPortfolioData(AppDbContext context, string userId)
{
    context.Database.ExecuteSqlRaw("UPDATE Assets SET UserId = {0} WHERE UserId = '' OR UserId IS NULL", userId);
    context.Database.ExecuteSqlRaw("UPDATE Budgets SET UserId = {0} WHERE UserId = '' OR UserId IS NULL", userId);
    context.Database.ExecuteSqlRaw("UPDATE Transactions SET UserId = {0} WHERE UserId = '' OR UserId IS NULL", userId);
    context.Database.ExecuteSqlRaw("UPDATE Tags SET UserId = {0} WHERE UserId = '' OR UserId IS NULL", userId);
}
