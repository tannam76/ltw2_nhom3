using CourseManagement.Data;
using CourseManagement.DTOs.Quiz;
using CourseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuizzesController(AppDbContext db) : ControllerBase
{
    // GET api/quizzes?lessonId=1
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuizDto>>> GetByLesson([FromQuery] int lessonId)
    {
        if (lessonId <= 0)
            return BadRequest(new { message = "lessonId is required." });

        var quizzes = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .Where(q => q.LessonId == lessonId)
            .Select(q => ToDto(q))
            .ToListAsync();

        return Ok(quizzes);
    }

    // GET api/quizzes/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuizDto>> GetById(int id)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        return quiz is null ? NotFound() : Ok(ToDto(quiz));
    }

    // POST api/quizzes
    [HttpPost]
    public async Task<ActionResult<QuizDto>> Create(CreateQuizDto dto)
    {
        var lessonExists = await db.Lessons.AnyAsync(l => l.Id == dto.LessonId);
        if (!lessonExists)
            return BadRequest(new { message = "Lesson not found." });

        var quiz = new Quiz
        {
            LessonId     = dto.LessonId,
            Title        = dto.Title.Trim(),
            PassingScore = dto.PassingScore,
            Questions    = dto.Questions.Select((q, qi) => new QuizQuestion
            {
                Question           = q.Question.Trim(),
                CorrectOptionIndex = q.CorrectOptionIndex,
                Options            = q.Options.Select((o, oi) => new QuizOption
                {
                    Text  = o.Text.Trim(),
                    Order = oi
                }).ToList()
            }).ToList()
        };

        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();

        await db.Entry(quiz).Collection(q => q.Questions).Query()
            .Include(q => q.Options).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, ToDto(quiz));
    }

    // PUT api/quizzes/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<QuizDto>> Update(int id, UpdateQuizDto dto)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz is null) return NotFound();

        quiz.Title        = dto.Title.Trim();
        quiz.PassingScore = dto.PassingScore;

        // Replace questions entirely
        db.QuizOptions.RemoveRange(quiz.Questions.SelectMany(q => q.Options));
        db.QuizQuestions.RemoveRange(quiz.Questions);

        quiz.Questions = dto.Questions.Select((q, qi) => new QuizQuestion
        {
            QuizId             = quiz.Id,
            Question           = q.Question.Trim(),
            CorrectOptionIndex = q.CorrectOptionIndex,
            Options            = q.Options.Select((o, oi) => new QuizOption
            {
                Text  = o.Text.Trim(),
                Order = oi
            }).ToList()
        }).ToList();

        await db.SaveChangesAsync();

        await db.Entry(quiz).Collection(q => q.Questions).Query()
            .Include(q => q.Options).LoadAsync();

        return Ok(ToDto(quiz));
    }

    // POST api/quizzes/{id}/submit  — nộp bài làm trắc nghiệm
    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id, [FromBody] QuizSubmitDto dto)
    {
        var quiz = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz is null) return NotFound();

        int correct = 0;
        var results = quiz.Questions.Select(q =>
        {
            dto.Answers.TryGetValue(q.Id, out int answered);
            bool isCorrect = answered == q.CorrectOptionIndex;
            if (isCorrect) correct++;
            return new { questionId = q.Id, isCorrect, correctIndex = q.CorrectOptionIndex };
        }).ToList();

        int total   = quiz.Questions.Count;
        int score   = total == 0 ? 0 : (int)Math.Round((double)correct / total * 100);
        bool passed = score >= quiz.PassingScore;

        return Ok(new { score, passed, correct, total, passingScore = quiz.PassingScore, results });
    }

    // DELETE api/quizzes/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var quiz = await db.Quizzes.FindAsync(id);
        if (quiz is null) return NotFound();

        db.Quizzes.Remove(quiz);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static QuizDto ToDto(Quiz q) => new()
    {
        Id           = q.Id,
        LessonId     = q.LessonId,
        Title        = q.Title,
        PassingScore = q.PassingScore,
        Questions    = q.Questions.Select(qq => new QuizQuestionDto
        {
            Id       = qq.Id,
            Question = qq.Question,
            Options  = qq.Options.OrderBy(o => o.Order).Select(o => new QuizOptionDto
            {
                Id   = o.Id,
                Text = o.Text
            }).ToList()
        }).ToList()
    };
}

public class QuizSubmitDto
{
    // key = questionId, value = chosen option index
    public Dictionary<int, int> Answers { get; set; } = [];
}