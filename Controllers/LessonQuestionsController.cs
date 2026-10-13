using CourseManagement.Data;
using CourseManagement.DTOs.LessonQuestion;
using CourseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/lesson-questions")]
public class LessonQuestionsController(AppDbContext db) : ControllerBase
{
    // ── INSTRUCTOR: Quản lý câu hỏi ───────────────────────────────────────────

    /// <summary>GET api/lesson-questions?lessonId=1  — Lấy tất cả câu hỏi của bài học</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LessonQuestionDto>>> GetByLesson(
        [FromQuery] int lessonId,
        [FromQuery] bool showCorrect = false)
    {
        if (lessonId <= 0)
            return BadRequest(new { message = "lessonId là bắt buộc." });

        var questions = await db.LessonQuestions
            .Include(q => q.Creator)
            .Include(q => q.Options)
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.Order)
            .ToListAsync();

        return Ok(questions.Select(q => ToDto(q, showCorrect)));
    }

    /// <summary>GET api/lesson-questions/{id}  — Lấy chi tiết câu hỏi</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LessonQuestionDto>> GetById(int id, [FromQuery] bool showCorrect = false)
    {
        var question = await db.LessonQuestions
            .Include(q => q.Creator)
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        return question is null ? NotFound() : Ok(ToDto(question, showCorrect));
    }

    /// <summary>POST api/lesson-questions  — Instructor tạo câu hỏi mới</summary>
    [HttpPost]
    public async Task<ActionResult<LessonQuestionDto>> Create(CreateLessonQuestionDto dto)
    {
        // Kiểm tra bài học tồn tại
        var lesson = await db.Lessons.FindAsync(dto.LessonId);
        if (lesson is null)
            return BadRequest(new { message = "Bài học không tồn tại." });

        // Kiểm tra người tạo tồn tại
        var creator = await db.Users.FindAsync(dto.CreatorId);
        if (creator is null)
            return BadRequest(new { message = "Người tạo không tồn tại." });

        // Parse loại câu hỏi
        if (!Enum.TryParse<QuestionType>(dto.Type, ignoreCase: true, out var type))
            return BadRequest(new { message = "Type phải là 'Essay' hoặc 'MultipleChoice'." });

        // Trắc nghiệm bắt buộc có ít nhất 2 đáp án
        if (type == QuestionType.MultipleChoice && dto.Options.Count < 2)
            return BadRequest(new { message = "Câu trắc nghiệm phải có ít nhất 2 lựa chọn." });

        // Trắc nghiệm bắt buộc có đúng 1 đáp án đúng
        if (type == QuestionType.MultipleChoice && dto.Options.Count(o => o.IsCorrect) != 1)
            return BadRequest(new { message = "Câu trắc nghiệm phải có đúng 1 đáp án đúng." });

        // Tự động gán Order nếu không nhập
        int nextOrder = dto.Order > 0
            ? dto.Order
            : (await db.LessonQuestions
                .Where(q => q.LessonId == dto.LessonId)
                .MaxAsync(q => (int?)q.Order) ?? 0) + 1;

        var question = new LessonQuestion
        {
            LessonId     = dto.LessonId,
            CreatorId    = dto.CreatorId,
            QuestionText = dto.QuestionText.Trim(),
            Type         = type,
            Order        = nextOrder,
            CreatedAt    = DateTime.UtcNow,
            Options      = type == QuestionType.MultipleChoice
                ? dto.Options.Select((o, i) => new LessonQuestionOption
                {
                    Text      = o.Text.Trim(),
                    IsCorrect = o.IsCorrect,
                    Order     = o.Order > 0 ? o.Order : i
                }).ToList()
                : []
        };

        db.LessonQuestions.Add(question);
        await db.SaveChangesAsync();

        // Reload để có Creator navigation
        await db.Entry(question).Reference(q => q.Creator).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = question.Id }, ToDto(question, showCorrect: true));
    }

    /// <summary>PUT api/lesson-questions/{id}  — Instructor chỉnh sửa câu hỏi</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<LessonQuestionDto>> Update(int id, UpdateLessonQuestionDto dto)
    {
        var question = await db.LessonQuestions
            .Include(q => q.Creator)
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question is null) return NotFound();

        question.QuestionText = dto.QuestionText.Trim();
        question.Order        = dto.Order > 0 ? dto.Order : question.Order;

        // Cập nhật đáp án nếu là trắc nghiệm
        if (question.Type == QuestionType.MultipleChoice)
        {
            if (dto.Options.Count < 2)
                return BadRequest(new { message = "Câu trắc nghiệm phải có ít nhất 2 lựa chọn." });
            if (dto.Options.Count(o => o.IsCorrect) != 1)
                return BadRequest(new { message = "Câu trắc nghiệm phải có đúng 1 đáp án đúng." });

            db.LessonQuestionOptions.RemoveRange(question.Options);
            question.Options = dto.Options.Select((o, i) => new LessonQuestionOption
            {
                LessonQuestionId = question.Id,
                Text      = o.Text.Trim(),
                IsCorrect = o.IsCorrect,
                Order     = o.Order > 0 ? o.Order : i
            }).ToList();
        }

        await db.SaveChangesAsync();
        return Ok(ToDto(question, showCorrect: true));
    }

    /// <summary>DELETE api/lesson-questions/{id}  — Instructor xóa câu hỏi</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var question = await db.LessonQuestions.FindAsync(id);
        if (question is null) return NotFound();

        db.LessonQuestions.Remove(question);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── STUDENT: Nộp câu trả lời ─────────────────────────────────────────────

    /// <summary>POST api/lesson-questions/{id}/answer  — Học sinh nộp câu trả lời</summary>
    [HttpPost("{id:int}/answer")]
    public async Task<ActionResult<LessonQuestionAnswerDto>> SubmitAnswer(int id, SubmitAnswerDto dto)
    {
        var question = await db.LessonQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound(new { message = "Câu hỏi không tồn tại." });

        var userExists = await db.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists) return BadRequest(new { message = "Người dùng không tồn tại." });

        // Validate theo loại câu hỏi
        if (question.Type == QuestionType.Essay)
        {
            if (string.IsNullOrWhiteSpace(dto.EssayText))
                return BadRequest(new { message = "Câu tự luận cần có nội dung trả lời." });
        }
        else
        {
            if (dto.SelectedOptionIndex is null)
                return BadRequest(new { message = "Câu trắc nghiệm cần chọn một đáp án." });
            if (dto.SelectedOptionIndex < 0 || dto.SelectedOptionIndex >= question.Options.Count)
                return BadRequest(new { message = "Chỉ số đáp án không hợp lệ." });
        }

        // Kiểm tra đã trả lời chưa
        var existing = await db.LessonQuestionAnswers
            .FirstOrDefaultAsync(a => a.LessonQuestionId == id && a.UserId == dto.UserId);

        bool isNew = existing is null;

        if (existing is not null)
        {
            // Cho phép cập nhật câu trả lời
            existing.EssayText           = dto.EssayText?.Trim();
            existing.SelectedOptionIndex = dto.SelectedOptionIndex;
            existing.SubmittedAt         = DateTime.UtcNow;
            existing.Score               = null;  // Reset điểm khi nộp lại
            existing.InstructorFeedback  = null;
            existing.GradedAt            = null;
        }
        else
        {
            existing = new LessonQuestionAnswer
            {
                LessonQuestionId     = id,
                UserId               = dto.UserId,
                EssayText            = dto.EssayText?.Trim(),
                SelectedOptionIndex  = dto.SelectedOptionIndex,
                SubmittedAt          = DateTime.UtcNow
            };
            db.LessonQuestionAnswers.Add(existing);
        }

        await db.SaveChangesAsync();

        // Tính kết quả cho trắc nghiệm
        bool? isCorrect = null;
        if (question.Type == QuestionType.MultipleChoice)
        {
            var correctOption = question.Options.OrderBy(o => o.Order).FirstOrDefault(o => o.IsCorrect);
            int correctIndex  = question.Options.OrderBy(o => o.Order).ToList().IndexOf(correctOption!);
            isCorrect = existing.SelectedOptionIndex == correctIndex;
        }

        await db.Entry(existing).Reference(a => a.User).LoadAsync();

        return isNew
            ? CreatedAtAction(nameof(GetAnswers), new { id }, ToAnswerDto(existing, isCorrect))
            : Ok(ToAnswerDto(existing, isCorrect));
    }

    // ── INSTRUCTOR: Xem & chấm điểm bài làm ─────────────────────────────────

    /// <summary>GET api/lesson-questions/{id}/answers  — Xem tất cả câu trả lời</summary>
    [HttpGet("{id:int}/answers")]
    public async Task<ActionResult<IEnumerable<LessonQuestionAnswerDto>>> GetAnswers(int id)
    {
        var question = await db.LessonQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound();

        var answers = await db.LessonQuestionAnswers
            .Include(a => a.User)
            .Where(a => a.LessonQuestionId == id)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        // Tính isCorrect cho trắc nghiệm
        int? correctIndex = null;
        if (question.Type == QuestionType.MultipleChoice)
        {
            var ordered = question.Options.OrderBy(o => o.Order).ToList();
            var correct = ordered.FirstOrDefault(o => o.IsCorrect);
            correctIndex = correct is null ? null : ordered.IndexOf(correct);
        }

        return Ok(answers.Select(a =>
        {
            bool? isCorrect = question.Type == QuestionType.MultipleChoice
                ? a.SelectedOptionIndex == correctIndex
                : (bool?)null;
            return ToAnswerDto(a, isCorrect);
        }));
    }

    /// <summary>GET api/lesson-questions/{id}/answers/user/{userId}  — Xem câu trả lời của 1 học sinh</summary>
    [HttpGet("{id:int}/answers/user/{userId:int}")]
    public async Task<ActionResult<LessonQuestionAnswerDto>> GetAnswerByUser(int id, int userId)
    {
        var question = await db.LessonQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound();

        var answer = await db.LessonQuestionAnswers
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.LessonQuestionId == id && a.UserId == userId);
        if (answer is null) return NotFound(new { message = "Học sinh chưa trả lời câu này." });

        bool? isCorrect = null;
        if (question.Type == QuestionType.MultipleChoice)
        {
            var ordered = question.Options.OrderBy(o => o.Order).ToList();
            var correct = ordered.FirstOrDefault(o => o.IsCorrect);
            int cIdx    = correct is null ? -1 : ordered.IndexOf(correct);
            isCorrect   = answer.SelectedOptionIndex == cIdx;
        }

        return Ok(ToAnswerDto(answer, isCorrect));
    }

    /// <summary>PATCH api/lesson-questions/answers/{answerId}/grade  — Instructor chấm điểm tự luận</summary>
    [HttpPatch("answers/{answerId:int}/grade")]
    public async Task<ActionResult<LessonQuestionAnswerDto>> GradeAnswer(int answerId, GradeAnswerDto dto)
    {
        var answer = await db.LessonQuestionAnswers
            .Include(a => a.User)
            .Include(a => a.LessonQuestion)
            .FirstOrDefaultAsync(a => a.Id == answerId);

        if (answer is null) return NotFound();

        if (answer.LessonQuestion.Type != QuestionType.Essay)
            return BadRequest(new { message = "Chỉ chấm điểm thủ công cho câu tự luận." });

        answer.Score              = dto.Score;
        answer.InstructorFeedback = dto.Feedback?.Trim();
        answer.GradedAt           = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToAnswerDto(answer, isCorrect: null));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LessonQuestionDto ToDto(LessonQuestion q, bool showCorrect)
    {
        var ordered = q.Options.OrderBy(o => o.Order).ToList();
        return new LessonQuestionDto
        {
            Id           = q.Id,
            LessonId     = q.LessonId,
            CreatorId    = q.CreatorId,
            CreatorName  = q.Creator?.FullName ?? string.Empty,
            QuestionText = q.QuestionText,
            Type         = q.Type.ToString(),
            Order        = q.Order,
            CreatedAt    = q.CreatedAt,
            Options      = ordered.Select(o => new LessonQuestionOptionDto
            {
                Id        = o.Id,
                Text      = o.Text,
                Order     = o.Order,
                IsCorrect = showCorrect ? o.IsCorrect : null   // ẩn với học sinh
            }).ToList()
        };
    }

    private static LessonQuestionAnswerDto ToAnswerDto(LessonQuestionAnswer a, bool? isCorrect) =>
        new()
        {
            Id                  = a.Id,
            LessonQuestionId    = a.LessonQuestionId,
            UserId              = a.UserId,
            UserName            = a.User?.FullName ?? string.Empty,
            EssayText           = a.EssayText,
            SelectedOptionIndex = a.SelectedOptionIndex,
            IsCorrect           = isCorrect,
            Score               = a.Score,
            InstructorFeedback  = a.InstructorFeedback,
            SubmittedAt         = a.SubmittedAt,
            GradedAt            = a.GradedAt
        };
}
