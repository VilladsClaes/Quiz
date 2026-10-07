using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuizApp.Data;

/// <summary>
/// Bruges kun af "dotnet ef" (design-time), fx naar der laves migrationer eller scripts.
/// Selve appen konfigurerer sin database i Program.cs ud fra miljoevariabler.
///
/// Saet EF_PROVIDER=sqlite for at generere/koere mod en lokal SQLite-fil,
/// ellers bruges MySQL (standard), som er det webhotellet koerer.
/// </summary>
public class QuizDbContextFactory : IDesignTimeDbContextFactory<QuizDbContext>
{
    public QuizDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuizDbContext>();
        var provider = (Environment.GetEnvironmentVariable("EF_PROVIDER") ?? "mysql").ToLowerInvariant();

        if (provider == "sqlite")
        {
            optionsBuilder.UseSqlite(
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Data Source=quizapp.db");
        }
        else
        {
            var version = new Version(Environment.GetEnvironmentVariable("MYSQL_VERSION") ?? "8.0.0");
            // Dummy-forbindelse: bruges kun til at forme SQL'en, der oprettes ingen forbindelse.
            optionsBuilder.UseMySql(
                "Server=localhost;Database=quizapp;Uid=root;Pwd=root;",
                new MySqlServerVersion(version));
        }

        return new QuizDbContext(optionsBuilder.Options);
    }
}
