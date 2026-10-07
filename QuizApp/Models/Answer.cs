using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models;

/// <summary>Et afgivet svar paa et enkelt spoergsmaal i en afleveret besvarelse.</summary>
public class Answer
{
    public int Id { get; set; }

    public int QuestionId { get; set; }
    public virtual Question? Question { get; set; }

    public int? QuizResultId { get; set; }
    public virtual QuizResult? QuizResult { get; set; }

    public string GivenText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Score { get; set; }
}
