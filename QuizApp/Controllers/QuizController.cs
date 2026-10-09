using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;
using QuizApp.ViewModels;

namespace QuizApp.Controllers;

/// <summary>Den offentlige del: se quizzer, spil dem og aflever.</summary>
public class QuizController : Controller
{
    private readonly QuizDbContext _db;

    public QuizController(QuizDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // Questions hentes med, saa kortene kan vise antal spoergsmaal.
        var quizzes = await _db.Quizzes
            .Where(q => q.IsActive)
            .OrderBy(q => q.Title)
            .Include(q => q.Questions)
            .ToListAsync();

        return View(new QuizListViewModel { Quizzes = quizzes });
    }

    public async Task<IActionResult> Play(int id)
    {
        var quiz = await LoadQuiz(id);
        if (quiz is null)
        {
            return NotFound();
        }

        return View(new QuizPlayViewModel
        {
            Quiz = quiz,
            Questions = quiz.Questions.OrderBy(q => q.Id).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id, QuizSubmissionViewModel contact)
    {
        var quiz = await LoadQuiz(id);
        if (quiz is null)
        {
            return NotFound();
        }

        var questions = quiz.Questions.OrderBy(q => q.Id).ToList();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Udfyld venligst dit navn (og evt. e-mail/telefon) korrekt, og tryk aflever igen.";
            return RedirectToAction(nameof(Play), new { id });
        }

        var scores = QuizScoring.Score(questions, Request.Form);

        var result = new QuizResult
        {
            QuizId = id,
            ParticipantName = contact.ParticipantName.Trim(),
            Email = contact.Email?.Trim(),
            Phone = contact.Phone?.Trim(),
            TotalPoints = questions.Sum(q => q.Points),
            Score = scores.Sum(s => s.Points),
            CompletedAt = DateTime.UtcNow
        };

        foreach (var s in scores)
        {
            result.Answers.Add(new Answer
            {
                QuestionId = s.QuestionId,
                GivenText = Trim(s.GivenText),
                IsCorrect = s.IsCorrect,
                Score = s.Points
            });
        }

        _db.QuizResults.Add(result);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Result), new { id = result.Id });
    }

    public async Task<IActionResult> Result(int id)
    {
        var result = await _db.QuizResults
            .Include(r => r.Quiz)
            .Include(r => r.Answers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q!.AnswerChoices)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (result is null)
        {
            return NotFound();
        }

        var vm = new QuizResultViewModel
        {
            Quiz = result.Quiz!,
            Result = result
        };

        foreach (var answer in result.Answers)
        {
            var question = answer.Question!;
            vm.Rows.Add(new QuestionScoreRow
            {
                Question = question,
                IsCorrect = answer.IsCorrect,
                Points = answer.Score,
                MaxPoints = question.Points,
                GivenText = answer.GivenText,
                CorrectText = QuizScoring.CorrectAnswerText(question)
            });
        }

        return View(vm);
    }

    private Task<Quiz?> LoadQuiz(int id) => _db.Quizzes
        .Include(q => q.Questions)
            .ThenInclude(q => q.AnswerChoices)
        .FirstOrDefaultAsync(q => q.Id == id);

    private static string Trim(string? value)
    {
        value ??= string.Empty;
        return value.Length <= 1000 ? value : value[..1000];
    }
}
