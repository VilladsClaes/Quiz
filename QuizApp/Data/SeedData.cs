using Microsoft.EntityFrameworkCore;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Data;

/// <summary>Opretter en admin-bruger og et par eksempelquizzer foerste gang.</summary>
public static class SeedData
{
    public static void EnsureSeeded(QuizDbContext db, IConfiguration config, ILogger logger)
    {
        SeedManager(db, config, logger);
        SeedQuizzes(db, logger);
    }

    private static void SeedManager(QuizDbContext db, IConfiguration config, ILogger logger)
    {
        if (db.QuizManagers.Any())
        {
            return;
        }

        var username = config["Admin:Username"] ?? "admin";
        var password = config["Admin:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            password = "Admin123!";
            logger.LogWarning(
                "Der er oprettet en standard-admin '{User}'. Saet miljoevariablen ADMIN_PASSWORD " +
                "(eller Admin__Password) og skift adgangskoden inden sitet bruges offentligt.",
                username);
        }

        db.QuizManagers.Add(new QuizManager
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password)
        });
        db.SaveChanges();
    }

    private static void SeedQuizzes(QuizDbContext db, ILogger logger)
    {
        if (db.Quizzes.Any())
        {
            return;
        }

        var general = new Quiz
        {
            Title = "Generel viden",
            Description = "En lille quiz der viser alle spoergsmaaltyperne."
        };

        general.Questions.Add(new Question
        {
            Text = "Hvad er hovedstaden i Danmark?",
            Type = QuestionType.MultipleChoiceSingleAnswer,
            Points = 1,
            AnswerChoices =
            {
                new AnswerChoice { Text = "Kobenhavn", IsCorrect = true, OrderIndex = 0 },
                new AnswerChoice { Text = "Aarhus", OrderIndex = 1 },
                new AnswerChoice { Text = "Odense", OrderIndex = 2 },
                new AnswerChoice { Text = "Aalborg", OrderIndex = 3 }
            }
        });

        general.Questions.Add(new Question
        {
            Text = "Hvilke af disse er nordiske lande? (flere svar)",
            Type = QuestionType.MultipleChoiceMultipleAnswers,
            Points = 2,
            AnswerChoices =
            {
                new AnswerChoice { Text = "Danmark", IsCorrect = true, OrderIndex = 0 },
                new AnswerChoice { Text = "Norge", IsCorrect = true, OrderIndex = 1 },
                new AnswerChoice { Text = "Holland", OrderIndex = 2 },
                new AnswerChoice { Text = "Sverige", IsCorrect = true, OrderIndex = 3 }
            }
        });

        general.Questions.Add(new Question
        {
            Text = "Jorden er rund.",
            Type = QuestionType.TrueFalse,
            Points = 1,
            AnswerChoices =
            {
                new AnswerChoice { Text = "Sandt", IsCorrect = true, OrderIndex = 0 },
                new AnswerChoice { Text = "Falsk", OrderIndex = 1 }
            }
        });

        general.Questions.Add(new Question
        {
            Text = "Hvad hedder Danmarks laengste flod (skriv navnet)?",
            Type = QuestionType.ShortAnswer,
            Points = 2,
            AnswerChoices =
            {
                new AnswerChoice { Text = "Gudenåen", IsCorrect = true, OrderIndex = 0 },
                new AnswerChoice { Text = "Gudenaaen", IsCorrect = true, OrderIndex = 1 }
            }
        });

        general.Questions.Add(new Question
        {
            Text = "Hvad er 12 x 12?",
            Type = QuestionType.NumericAnswer,
            Points = 1,
            AnswerChoices =
            {
                new AnswerChoice { Text = "144", IsCorrect = true, OrderIndex = 0 }
            }
        });

        general.Questions.Add(new Question
        {
            Text = "Saet byerne i raekkefoelge fra nord til syd.",
            Type = QuestionType.Ordering,
            Points = 3,
            AnswerChoices =
            {
                new AnswerChoice { Text = "Aalborg", OrderIndex = 0 },
                new AnswerChoice { Text = "Aarhus", OrderIndex = 1 },
                new AnswerChoice { Text = "Odense", OrderIndex = 2 },
                new AnswerChoice { Text = "Kobenhavn", OrderIndex = 3 }
            }
        });

        general.Questions.Add(new Question
        {
            Text = "Match hvert land med dets hovedstad.",
            Type = QuestionType.MatchingPairs,
            Points = 3,
            AnswerChoices =
            {
                new AnswerChoice { Text = "Danmark|Kobenhavn", OrderIndex = 0 },
                new AnswerChoice { Text = "Norge|Oslo", OrderIndex = 1 },
                new AnswerChoice { Text = "Sverige|Stockholm", OrderIndex = 2 }
            }
        });

        general.Questions.Add(new Question
        {
            Text = "Hvilket dyr er et pattedyr?",
            Type = QuestionType.ImageChoice,
            Points = 1,
            AnswerChoices =
            {
                new AnswerChoice { Text = "Hval", IsCorrect = true, OrderIndex = 0 },
                new AnswerChoice { Text = "Haj", OrderIndex = 1 },
                new AnswerChoice { Text = "Tun", OrderIndex = 2 }
            }
        });

        db.Quizzes.Add(general);
        db.SaveChanges();
        logger.LogInformation("Eksempelquizzen 'Generel viden' blev oprettet.");
    }
}
