using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models;

/// <summary>En administrator der kan oprette og redigere quizzer.</summary>
public class QuizManager
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime Created { get; set; } = DateTime.UtcNow;
}
