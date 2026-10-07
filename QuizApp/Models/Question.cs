using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models;

public enum QuestionType
{
    MultipleChoiceSingleAnswer,
    MultipleChoiceMultipleAnswers,
    TrueFalse,
    ShortAnswer,
    NumericAnswer,
    MatchingPairs,
    Ordering,
    ImageChoice
}

public class Question
{
    public int Id { get; set; }

    public int QuizId { get; set; }
    public virtual Quiz? Quiz { get; set; }

    [Required]
    [StringLength(1000)]
    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; } = QuestionType.MultipleChoiceSingleAnswer;

    public int Points { get; set; } = 1;

    public int? TimeLimitSeconds { get; set; }

    public virtual ICollection<AnswerChoice> AnswerChoices { get; set; } = new List<AnswerChoice>();
    public virtual ICollection<Answer> GivenAnswers { get; set; } = new List<Answer>();
}
