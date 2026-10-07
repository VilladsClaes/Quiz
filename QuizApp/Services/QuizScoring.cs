using System.Globalization;
using Microsoft.AspNetCore.Http;
using QuizApp.Models;

namespace QuizApp.Services;

/// <summary>Resultatet for et enkelt spoergsmaal efter aflevering.</summary>
public class QuestionScore
{
    public int QuestionId { get; set; }
    public int Points { get; set; }
    public int MaxPoints { get; set; }
    public bool IsCorrect { get; set; }
    public string? GivenText { get; set; }
}

/// <summary>
/// Retter en hel quiz ud fra de vaerdier brugeren har sendt i formularen.
/// Alle spoergsmaaltyper er samlet her, saa baade aflevering og visning af
/// resultat bruger praecis samme regler.
///
/// Formularfelter:
///   q_{id}                  tekst / tal (ShortAnswer, NumericAnswer)
///   choice_{id}             ét valgt AnswerChoice-Id (single, true/false, billede)
///   choices_{id}            flere valgte AnswerChoice-Id'er (multiple answers)
///   order_{id}              komma-separeret raekkefoelge af AnswerChoice-Id'er (ordering)
///   match_{id}_{choiceId}   den valgte hoejre-side for en venstre-side (matching pairs)
/// </summary>
public static class QuizScoring
{
    public static List<QuestionScore> Score(IEnumerable<Question> questions, IFormCollection form)
    {
        var results = new List<QuestionScore>();

        foreach (var q in questions)
        {
            var result = new QuestionScore
            {
                QuestionId = q.Id,
                MaxPoints = q.Points
            };

            switch (q.Type)
            {
                case QuestionType.MultipleChoiceSingleAnswer:
                case QuestionType.TrueFalse:
                case QuestionType.ImageChoice:
                    ScoreSingleChoice(q, form, result);
                    break;

                case QuestionType.MultipleChoiceMultipleAnswers:
                    ScoreMultipleChoice(q, form, result);
                    break;

                case QuestionType.ShortAnswer:
                    ScoreShortAnswer(q, form, result);
                    break;

                case QuestionType.NumericAnswer:
                    ScoreNumeric(q, form, result);
                    break;

                case QuestionType.Ordering:
                    ScoreOrdering(q, form, result);
                    break;

                case QuestionType.MatchingPairs:
                    ScoreMatchingPairs(q, form, result);
                    break;

                default:
                    ScoreSingleChoice(q, form, result);
                    break;
            }

            results.Add(result);
        }

        return results;
    }

    private static void ScoreSingleChoice(Question q, IFormCollection form, QuestionScore result)
    {
        var raw = form[$"choice_{q.Id}"].ToString();
        var selected = int.TryParse(raw, out var id) ? id : 0;
        var choice = q.AnswerChoices.FirstOrDefault(c => c.Id == selected);
        result.GivenText = choice?.Text;
        result.IsCorrect = choice?.IsCorrect ?? false;
        result.Points = result.IsCorrect ? q.Points : 0;
    }

    private static void ScoreMultipleChoice(Question q, IFormCollection form, QuestionScore result)
    {
        var selected = form[$"choices_{q.Id}"]
            .Select(v => int.TryParse(v, out var id) ? id : 0)
            .Where(id => id != 0)
            .ToHashSet();
        var correct = q.AnswerChoices.Where(c => c.IsCorrect).Select(c => c.Id).ToHashSet();

        result.GivenText = string.Join(", ", q.AnswerChoices.Where(c => selected.Contains(c.Id)).Select(c => c.Text));
        result.IsCorrect = selected.Count > 0 && selected.SetEquals(correct);
        result.Points = result.IsCorrect ? q.Points : 0;
    }

    private static void ScoreShortAnswer(Question q, IFormCollection form, QuestionScore result)
    {
        var given = (form[$"q_{q.Id}"].ToString() ?? string.Empty).Trim();
        result.GivenText = given;
        result.IsCorrect = q.AnswerChoices
            .Where(c => c.IsCorrect)
            .Any(c => string.Equals(c.Text.Trim(), given, StringComparison.OrdinalIgnoreCase));
        result.Points = result.IsCorrect ? q.Points : 0;
    }

    private static void ScoreNumeric(Question q, IFormCollection form, QuestionScore result)
    {
        var given = (form[$"q_{q.Id}"].ToString() ?? string.Empty).Trim().Replace(',', '.');
        result.GivenText = given;

        var parsed = double.TryParse(given, NumberStyles.Any, CultureInfo.InvariantCulture, out var value);
        result.IsCorrect = parsed && q.AnswerChoices.Where(c => c.IsCorrect).Any(c =>
            double.TryParse(c.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var expected)
            && Math.Abs(expected - value) < 0.0001);
        result.Points = result.IsCorrect ? q.Points : 0;
    }

    private static void ScoreOrdering(Question q, IFormCollection form, QuestionScore result)
    {
        var submitted = (form[$"order_{q.Id}"].ToString() ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => int.TryParse(v, out var id) ? id : 0)
            .Where(id => id != 0)
            .ToList();

        var correct = q.AnswerChoices.OrderBy(c => c.OrderIndex).Select(c => c.Id).ToList();

        result.IsCorrect = submitted.Count == correct.Count && submitted.SequenceEqual(correct);
        result.GivenText = string.Join(" -> ",
            submitted.Select(id => q.AnswerChoices.FirstOrDefault(c => c.Id == id)?.Text).Where(t => t != null));
        result.Points = result.IsCorrect ? q.Points : 0;
    }

    private static void ScoreMatchingPairs(Question q, IFormCollection form, QuestionScore result)
    {
        var lefts = q.AnswerChoices.OrderBy(c => c.OrderIndex).ToList();
        var pairsCorrect = 0;

        foreach (var left in lefts)
        {
            var expected = SplitPair(left.Text).Right;
            var given = form[$"match_{q.Id}_{left.Id}"].ToString();
            if (string.Equals(expected.Trim(), (given ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
            {
                pairsCorrect++;
            }
        }

        result.IsCorrect = lefts.Count > 0 && pairsCorrect == lefts.Count;
        result.GivenText = $"{pairsCorrect}/{lefts.Count} par korrekte";
        result.Points = lefts.Count == 0
            ? 0
            : (int)Math.Round(q.Points * (double)pairsCorrect / lefts.Count);
    }

    /// <summary>Deler en "venstre|hoejre"-tekst op i sine to dele.</summary>
    public static (string Left, string Right) SplitPair(string text)
    {
        var parts = text.Split('|', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (text, string.Empty);
    }

    /// <summary>Menneske-laesbar tekst med det korrekte svar, brugt paa resultatsiden.</summary>
    public static string CorrectAnswerText(Question q)
    {
        return q.Type switch
        {
            QuestionType.MatchingPairs => string.Join(", ",
                q.AnswerChoices.OrderBy(c => c.OrderIndex)
                    .Select(c => { var p = SplitPair(c.Text); return $"{p.Left} -> {p.Right}"; })),
            QuestionType.Ordering => string.Join(" -> ",
                q.AnswerChoices.OrderBy(c => c.OrderIndex).Select(c => c.Text)),
            _ => string.Join(", ",
                q.AnswerChoices.Where(c => c.IsCorrect).Select(c => c.Text))
        };
    }
}
