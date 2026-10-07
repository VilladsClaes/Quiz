using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models;

/// <summary>En afleveret besvarelse med deltagerens kontaktoplysninger (ingen login).</summary>
public class QuizResult
{
    public int Id { get; set; }

    public int QuizId { get; set; }
    public virtual Quiz? Quiz { get; set; }

    [Required]
    [StringLength(100)]
    public string ParticipantName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? Phone { get; set; }

    public int Score { get; set; }
    public int TotalPoints { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
