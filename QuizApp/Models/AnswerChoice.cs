using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models;

public class AnswerChoice
{
    public int Id { get; set; }

    public int QuestionId { get; set; }
    public virtual Question? Question { get; set; }

    [Required]
    [StringLength(1000)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }

    public string? ImageUrl { get; set; }
}
