using Microsoft.EntityFrameworkCore;
using QuizApp.Models;

namespace QuizApp.Data;

public class QuizDbContext : DbContext
{
    public QuizDbContext(DbContextOptions<QuizDbContext> options) : base(options)
    {
    }

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerChoice> AnswerChoices => Set<AnswerChoice>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<QuizManager> QuizManagers => Set<QuizManager>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasMany(e => e.Questions)
                  .WithOne(e => e.Quiz)
                  .HasForeignKey(e => e.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Results)
                  .WithOne(e => e.Quiz)
                  .HasForeignKey(e => e.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.Property(e => e.Text).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
            entity.HasMany(e => e.AnswerChoices)
                  .WithOne(e => e.Question)
                  .HasForeignKey(e => e.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.GivenAnswers)
                  .WithOne(e => e.Question)
                  .HasForeignKey(e => e.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnswerChoice>(entity =>
        {
            entity.Property(e => e.Text).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.HasIndex(e => new { e.QuestionId, e.OrderIndex });
        });

        modelBuilder.Entity<Answer>(entity =>
        {
            entity.Property(e => e.GivenText).IsRequired().HasMaxLength(1000);
            entity.HasOne(e => e.QuizResult)
                  .WithMany(r => r.Answers)
                  .HasForeignKey(e => e.QuizResultId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizResult>(entity =>
        {
            entity.Property(e => e.ParticipantName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(200);
        });

        modelBuilder.Entity<QuizManager>(entity =>
        {
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique();
        });
    }
}
