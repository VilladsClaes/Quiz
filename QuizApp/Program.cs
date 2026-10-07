using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database
// Saettes ConnectionStrings__DefaultConnection (fx som GitHub Secret eller
// miljoevariabel paa webhotellet) bruges MySQL. Ellers falder vi tilbage til en
// lokal SQLite-fil, saa appen kan koere og testes uden en MySQL-server.
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useMySql = !string.IsNullOrWhiteSpace(connectionString)
               && !connectionString.Contains("${", StringComparison.Ordinal);

builder.Services.AddDbContext<QuizDbContext>(options =>
{
    if (useMySql)
    {
        var provider = (builder.Configuration["Database:Provider"] ?? "mysql").ToLowerInvariant();
        var version = new Version(builder.Configuration["Database:ServerVersion"] ?? "8.0.0");
        var autoDetect = !string.Equals(builder.Configuration["Database:AutoDetect"], "false", StringComparison.OrdinalIgnoreCase);

        ServerVersion serverVersion;
        try
        {
            // Sporg serveren hvilken version den koerer (MySQL eller MariaDB),
            // saa Pomelo bruger de rigtige SQL-funktioner. Kan vi ikke naa den,
            // falder vi tilbage til den konfigurerede version.
            serverVersion = autoDetect
                ? ServerVersion.AutoDetect(connectionString)
                : BuildServerVersion(provider, version);
        }
        catch (Exception)
        {
            serverVersion = BuildServerVersion(provider, version);
        }

        options.UseMySql(connectionString, serverVersion,
            my => my.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }
    else
    {
        options.UseSqlite(
            builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=quizapp.db",
            sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }
});

builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.Name = "QuizApp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// ---------------------------------------------------------------------------
// Database init: migreringer paa MySQL (produktion) og EnsureCreated paa SQLite
// (lokal udvikling), efterfulgt af seed af admin-bruger og eksempelquizzer.
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
    if (db.Database.IsSqlite())
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }

    SeedData.EnsureSeeded(db, app.Configuration, app.Logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static ServerVersion BuildServerVersion(string provider, Version version)
    => provider == "mariadb" ? new MariaDbServerVersion(version) : new MySqlServerVersion(version);
