using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models;

public class Quiz
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<QuizResult> Results { get; set; } = new List<QuizResult>();
}
