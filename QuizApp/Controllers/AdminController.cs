using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;
using QuizApp.ViewModels;

namespace QuizApp.Controllers;

/// <summary>Administration af quizzer. Kraever login som quiz-manager.</summary>
[Authorize]
public class AdminController : Controller
{
    private readonly QuizDbContext _db;

    public AdminController(QuizDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        var manager = await _db.QuizManagers.FirstOrDefaultAsync(m => m.Username == username);
        if (manager is null || !PasswordHasher.Verify(password, manager.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Forkert brugernavn eller adgangskode.");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, manager.Username),
            new(ClaimTypes.NameIdentifier, manager.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Index()
    {
        var quizzes = await _db.Quizzes
            .Include(q => q.Questions)
            .OrderBy(q => q.Title)
            .ToListAsync();
        return View(quizzes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Titlen maa ikke vaere tom.";
            return RedirectToAction(nameof(Index));
        }

        var quiz = new Quiz { Title = title.Trim(), Description = description?.Trim() };
        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = quiz.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.Id == id);
        return quiz is null ? NotFound() : View(quiz);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string title, string? description, bool isActive)
    {
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.Id == id);
        if (quiz is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError(nameof(title), "Titlen maa ikke vaere tom.");
            return View(quiz);
        }

        quiz.Title = title.Trim();
        quiz.Description = description?.Trim();
        quiz.IsActive = isActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Quizzen er gemt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.Id == id);
        if (quiz is not null)
        {
            _db.Quizzes.Remove(quiz);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Questions(int id)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.AnswerChoices)
            .FirstOrDefaultAsync(q => q.Id == id);

        return quiz is null ? NotFound() : View(quiz);
    }

    [HttpGet]
    public async Task<IActionResult> Question(int id, int? questionId)
    {
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.Id == id);
        if (quiz is null)
        {
            return NotFound();
        }

        var vm = new AdminQuestionViewModel { QuizId = quiz.Id, QuizTitle = quiz.Title };

        if (questionId is not null)
        {
            var q = await _db.Questions
                .Include(x => x.AnswerChoices)
                .FirstOrDefaultAsync(x => x.Id == questionId && x.QuizId == id);

            if (q is null)
            {
                return NotFound();
            }

            vm.QuestionId = q.Id;
            vm.Text = q.Text;
            vm.Type = q.Type;
            vm.Points = q.Points;
            vm.TimeLimitSeconds = q.TimeLimitSeconds;
            vm.Choices = string.Join(Environment.NewLine, q.AnswerChoices
                .OrderBy(c => c.OrderIndex)
                .Select(c => (c.IsCorrect && q.Type is not QuestionType.MatchingPairs and not QuestionType.Ordering ? "*" : "") + c.Text));
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Question(AdminQuestionViewModel vm)
    {
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.Id == vm.QuizId);
        if (quiz is null)
        {
            return NotFound();
        }

        vm.QuizTitle = quiz.Title;

        if (string.IsNullOrWhiteSpace(vm.Text))
        {
            ModelState.AddModelError(nameof(vm.Text), "Spoergsmaalet maa ikke vaere tomt.");
            return View(vm);
        }

        Question question;
        if (vm.QuestionId is not null)
        {
            var existing = await _db.Questions
                .Include(x => x.AnswerChoices)
                .FirstOrDefaultAsync(x => x.Id == vm.QuestionId && x.QuizId == vm.QuizId);

            if (existing is null)
            {
                return NotFound();
            }

            question = existing;
            question.AnswerChoices.Clear();
        }
        else
        {
            question = new Question { QuizId = vm.QuizId };
            _db.Questions.Add(question);
        }

        question.Text = vm.Text.Trim();
        question.Type = vm.Type;
        question.Points = vm.Points < 1 ? 1 : vm.Points;
        question.TimeLimitSeconds = vm.TimeLimitSeconds;

        var index = 0;
        foreach (var raw in (vm.Choices ?? string.Empty)
                     .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var isCorrect = raw.StartsWith('*');
            var text = (isCorrect ? raw[1..] : raw).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            question.AnswerChoices.Add(new AnswerChoice
            {
                Text = text,
                IsCorrect = isCorrect || vm.Type is QuestionType.MatchingPairs or QuestionType.Ordering,
                OrderIndex = index++
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Spoergsmaalet er gemt.";
        return RedirectToAction(nameof(Questions), new { id = vm.QuizId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id, int questionId)
    {
        var question = await _db.Questions.FirstOrDefaultAsync(q => q.Id == questionId && q.QuizId == id);
        if (question is not null)
        {
            _db.Questions.Remove(question);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Questions), new { id });
    }

    public async Task<IActionResult> Results(int id)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Results)
            .FirstOrDefaultAsync(q => q.Id == id);

        return quiz is null ? NotFound() : View(quiz);
    }
}
