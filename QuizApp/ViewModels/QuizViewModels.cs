using System.ComponentModel.DataAnnotations;
using QuizApp.Models;

namespace QuizApp.ViewModels;

/// <summary>Oversigt over de quizzer en deltager kan starte.</summary>
public class QuizListViewModel
{
    public List<Quiz> Quizzes { get; set; } = new();
}

/// <summary>Selve spil-siden: en quiz med alle spoergsmaal og svarvalg.</summary>
public class QuizPlayViewModel
{
    public Quiz Quiz { get; set; } = null!;
    public List<Question> Questions { get; set; } = new();
}

/// <summary>Kontaktoplysninger der indsamles ved aflevering (ingen login for deltagere).</summary>
public class QuizSubmissionViewModel
{
    public int QuizId { get; set; }

    [Required(ErrorMessage = "Skriv dit navn")]
    [StringLength(100)]
    public string ParticipantName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Skriv en gyldig e-mail")]
    [StringLength(200)]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Skriv et gyldigt telefonnummer")]
    [StringLength(200)]
    public string? Phone { get; set; }
}

/// <summary>Resultatet efter en aflevering.</summary>
public class QuizResultViewModel
{
    public Quiz Quiz { get; set; } = null!;
    public QuizResult Result { get; set; } = null!;
    public List<QuestionScoreRow> Rows { get; set; } = new();
    public int Percent => Result.TotalPoints == 0 ? 0 : (int)Math.Round(100.0 * Result.Score / Result.TotalPoints);
}

public class QuestionScoreRow
{
    public Question Question { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int Points { get; set; }
    public int MaxPoints { get; set; }
    public string? GivenText { get; set; }
    public string? CorrectText { get; set; }
}

/// <summary>Redigering af et enkelt spoergsmaal i admin.</summary>
public class AdminQuestionViewModel
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;

    public int? QuestionId { get; set; }

    [Required(ErrorMessage = "Skriv spoergsmaalet")]
    [StringLength(1000)]
    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; } = QuestionType.MultipleChoiceSingleAnswer;

    [Range(1, 1000, ErrorMessage = "Point skal vaere mindst 1")]
    public int Points { get; set; } = 1;

    [Range(1, 3600, ErrorMessage = "Tidsgraense skal vaere mellem 1 og 3600 sekunder")]
    public int? TimeLimitSeconds { get; set; }

    /// <summary>
    /// Svarvalg, et pr. linje.
    /// * foran linjen markerer det korrekte svar (valg, tekst og tal).
    /// Ved "match par" skrives "venstre|hoejre" og ved "raekkefoelge" skrives de i den rigtige orden.
    /// </summary>
    public string? Choices { get; set; }
}
