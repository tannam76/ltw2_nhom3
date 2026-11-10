using CourseManagement.Data;
using CourseManagement.DTOs.Lesson;
using CourseManagement.DTOs.LessonQuestion;
using CourseManagement.Models;
using CourseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonsController(
    AppDbContext db,
    ILessonQuestionGeneratorService questionGenerator) : ControllerBase
{
    // GET api/lessons?courseId=1
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetByCourse([FromQuery] int courseId)
    {
        if (courseId <= 0)
            return BadRequest(new { message = "courseId is required." });

        var lessons = await db.Lessons
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.Order)
            .Select(l => new LessonDto
            {
                Id            = l.Id,
                CourseId      = l.CourseId,
                Title         = l.Title,
                Content       = l.Content,
                VideoUrl      = l.VideoUrl,
                Order         = l.Order,
                IsPublished   = l.IsPublished,
                QuestionCount = db.LessonQuestions.Count(q => q.LessonId == l.Id)
            })
            .ToListAsync();

        return Ok(lessons);
    }

    // GET api/lessons/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LessonDto>> GetById(int id, [FromQuery] bool includeQuestions = false)
    {
        var lesson = await db.Lessons.FindAsync(id);
        if (lesson is null) return NotFound();

        var questionCount = await db.LessonQuestions.CountAsync(q => q.LessonId == id);
        List<LessonQuestionDto>? questions = null;

        if (includeQuestions)
        {
            questions = await db.LessonQuestions
                .Where(q => q.LessonId == id)
                .OrderBy(q => q.Order)
                .Include(q => q.Creator)
                .Include(q => q.Options)
                .Select(q => new LessonQuestionDto
                {
                    Id           = q.Id,
                    LessonId     = q.LessonId,
                    CreatorId    = q.CreatorId,
                    CreatorName  = q.Creator.FullName,
                    QuestionText = q.QuestionText,
                    Type         = q.Type.ToString(),
                    Order        = q.Order,
                    CreatedAt    = q.CreatedAt,
                    AnswerCount  = db.LessonQuestionAnswers.Count(a => a.LessonQuestionId == q.Id),
                    Options      = q.Options.OrderBy(o => o.Order).Select(o => new LessonQuestionOptionDto
                    {
                        Id        = o.Id,
                        Text      = o.Text,
                        IsCorrect = o.IsCorrect,
                        Order     = o.Order
                    }).ToList()
                })
                .ToListAsync();
        }

        var dto = new LessonDto
        {
            Id            = lesson.Id,
            CourseId      = lesson.CourseId,
            Title         = lesson.Title,
            Content       = lesson.Content,
            VideoUrl      = lesson.VideoUrl,
            Order         = lesson.Order,
            IsPublished   = lesson.IsPublished,
            QuestionCount = questionCount,
            Questions     = questions
        };

        return Ok(dto);
    }

    // POST api/lessons
    // Hỗ trợ tự động tạo câu hỏi hỏi đáp (Tự luận / Trắc nghiệm) nếu người tạo chọn AutoGenerateQuestions = true
    [HttpPost]
    public async Task<ActionResult<LessonDto>> Create(CreateLessonDto dto)
    {
        var course = await db.Courses.FindAsync(dto.CourseId);
        if (course is null)
            return BadRequest(new { message = "Khóa học không tồn tại." });

        // Tự gán Order nếu không nhập
        int nextOrder = dto.Order > 0
            ? dto.Order
            : (await db.Lessons.Where(l => l.CourseId == dto.CourseId).MaxAsync(l => (int?)l.Order) ?? 0) + 1;

        var lesson = new Lesson
        {
            CourseId    = dto.CourseId,
            Title       = dto.Title.Trim(),
            Content     = dto.Content,
            VideoUrl    = dto.VideoUrl,
            Order       = nextOrder,
            IsPublished = false
        };

        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();

        List<LessonQuestionDto> generatedDtos = [];

        // Nếu người tạo muốn tự động tạo phần hỏi đáp dựa theo nội dung bài học
        if (dto.AutoGenerateQuestions && !string.IsNullOrWhiteSpace(dto.Content))
        {
            int creatorId = dto.CreatorId ?? course.InstructorId;
            var generatedQuestions = questionGenerator.GenerateQuestionsFromContent(
                lessonId: lesson.Id,
                lessonTitle: lesson.Title,
                lessonContent: dto.Content,
                creatorId: creatorId,
                count: dto.AutoQuestionCount,
                preferredType: dto.AutoQuestionType
            );

            if (generatedQuestions.Count > 0)
            {
                db.LessonQuestions.AddRange(generatedQuestions);
                await db.SaveChangesAsync();

                var creator = await db.Users.FindAsync(creatorId);
                var creatorName = creator?.FullName ?? "Giảng viên";

                generatedDtos = generatedQuestions.Select(q => new LessonQuestionDto
                {
                    Id           = q.Id,
                    LessonId     = q.LessonId,
                    CreatorId    = q.CreatorId,
                    CreatorName  = creatorName,
                    QuestionText = q.QuestionText,
                    Type         = q.Type.ToString(),
                    Order        = q.Order,
                    CreatedAt    = q.CreatedAt,
                    AnswerCount  = 0,
                    Options      = q.Options.Select(o => new LessonQuestionOptionDto
                    {
                        Id        = o.Id,
                        Text      = o.Text,
                        IsCorrect = o.IsCorrect,
                        Order     = o.Order
                    }).ToList()
                }).ToList();
            }
        }

        var result = new LessonDto
        {
            Id            = lesson.Id,
            CourseId      = lesson.CourseId,
            Title         = lesson.Title,
            Content       = lesson.Content,
            VideoUrl      = lesson.VideoUrl,
            Order         = lesson.Order,
            IsPublished   = lesson.IsPublished,
            QuestionCount = generatedDtos.Count,
            Questions     = generatedDtos.Count > 0 ? generatedDtos : null
        };

        return CreatedAtAction(nameof(GetById), new { id = lesson.Id }, result);
    }

    /// <summary>
    /// POST api/lessons/{id}/auto-generate-questions
    /// Chức năng sinh câu hỏi tự động (Tự luận và/hoặc Trắc nghiệm) dựa trên nội dung bài học hiện tại
    /// </summary>
    [HttpPost("{id:int}/auto-generate-questions")]
    public async Task<ActionResult<List<LessonQuestionDto>>> AutoGenerateQuestions(
        int id,
        [FromBody] AutoGenerateQuestionsRequestDto dto)
    {
        var lesson = await db.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
        if (lesson is null)
            return NotFound(new { message = "Bài học không tồn tại." });

        if (string.IsNullOrWhiteSpace(lesson.Content))
            return BadRequest(new { message = "Bài học này chưa có nội dung (Content) để tạo câu hỏi tự động." });

        int creatorId = dto.CreatorId ?? lesson.Course.InstructorId;
        var creator = await db.Users.FindAsync(creatorId);
        var creatorName = creator?.FullName ?? "Giảng viên";

        // Tự động sinh câu hỏi từ nội dung
        var generatedQuestions = questionGenerator.GenerateQuestionsFromContent(
            lessonId: lesson.Id,
            lessonTitle: lesson.Title,
            lessonContent: lesson.Content,
            creatorId: creatorId,
            count: dto.Count,
            preferredType: dto.Type
        );

        if (generatedQuestions.Count == 0)
            return BadRequest(new { message = "Không thể trích xuất câu hỏi từ nội dung được cung cấp." });

        if (dto.SaveToLesson)
        {
            // Tìm số thứ tự tiếp theo nếu bài học đã có sẵn câu hỏi
            int maxOrder = await db.LessonQuestions
                .Where(q => q.LessonId == id)
                .MaxAsync(q => (int?)q.Order) ?? 0;

            foreach (var q in generatedQuestions)
            {
                maxOrder++;
                q.Order = maxOrder;
            }

            db.LessonQuestions.AddRange(generatedQuestions);
            await db.SaveChangesAsync();
        }

        var result = generatedQuestions.Select(q => new LessonQuestionDto
        {
            Id           = q.Id,
            LessonId     = q.LessonId,
            CreatorId    = q.CreatorId,
            CreatorName  = creatorName,
            QuestionText = q.QuestionText,
            Type         = q.Type.ToString(),
            Order        = q.Order,
            CreatedAt    = q.CreatedAt,
            AnswerCount  = 0,
            Options      = q.Options.Select(o => new LessonQuestionOptionDto
            {
                Id        = o.Id,
                Text      = o.Text,
                IsCorrect = o.IsCorrect,
                Order     = o.Order
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// POST api/lessons/preview-questions
    /// Xem trước câu hỏi gợi ý từ nội dung đang nhập (chưa cần lưu vào database)
    /// </summary>
    [HttpPost("preview-questions")]
    public ActionResult<List<LessonQuestionDto>> PreviewQuestions([FromBody] PreviewQuestionsRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Nội dung bài học không được để trống." });

        var generatedQuestions = questionGenerator.GenerateQuestionsFromContent(
            lessonId: 0,
            lessonTitle: string.IsNullOrWhiteSpace(dto.Title) ? "Bài học" : dto.Title,
            lessonContent: dto.Content,
            creatorId: 0,
            count: dto.Count,
            preferredType: dto.Type
        );

        var result = generatedQuestions.Select(q => new LessonQuestionDto
        {
            Id           = 0,
            LessonId     = 0,
            CreatorId    = 0,
            CreatorName  = "Hệ thống gợi ý",
            QuestionText = q.QuestionText,
            Type         = q.Type.ToString(),
            Order        = q.Order,
            CreatedAt    = q.CreatedAt,
            AnswerCount  = 0,
            Options      = q.Options.Select(o => new LessonQuestionOptionDto
            {
                Id        = 0,
                Text      = o.Text,
                IsCorrect = o.IsCorrect,
                Order     = o.Order
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// GET api/lessons/{id}/questions
    /// Lấy toàn bộ câu hỏi hỏi đáp của bài học này
    /// </summary>
    [HttpGet("{id:int}/questions")]
    public async Task<ActionResult<List<LessonQuestionDto>>> GetLessonQuestions(int id)
    {
        var lessonExists = await db.Lessons.AnyAsync(l => l.Id == id);
        if (!lessonExists)
            return NotFound(new { message = "Bài học không tồn tại." });

        var questions = await db.LessonQuestions
            .Where(q => q.LessonId == id)
            .OrderBy(q => q.Order)
            .Include(q => q.Creator)
            .Include(q => q.Options)
            .Select(q => new LessonQuestionDto
            {
                Id           = q.Id,
                LessonId     = q.LessonId,
                CreatorId    = q.CreatorId,
                CreatorName  = q.Creator.FullName,
                QuestionText = q.QuestionText,
                Type         = q.Type.ToString(),
                Order        = q.Order,
                CreatedAt    = q.CreatedAt,
                AnswerCount  = db.LessonQuestionAnswers.Count(a => a.LessonQuestionId == q.Id),
                Options      = q.Options.OrderBy(o => o.Order).Select(o => new LessonQuestionOptionDto
                {
                    Id        = o.Id,
                    Text      = o.Text,
                    IsCorrect = o.IsCorrect,
                    Order     = o.Order
                }).ToList()
            })
            .ToListAsync();

        return Ok(questions);
    }

    // PUT api/lessons/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<LessonDto>> Update(int id, UpdateLessonDto dto)
    {
        var lesson = await db.Lessons.FindAsync(id);
        if (lesson is null) return NotFound();

        lesson.Title       = dto.Title.Trim();
        lesson.Content     = dto.Content;
        lesson.VideoUrl    = dto.VideoUrl;
        lesson.Order       = dto.Order;
        lesson.IsPublished = dto.IsPublished;

        await db.SaveChangesAsync();

        var questionCount = await db.LessonQuestions.CountAsync(q => q.LessonId == id);

        return Ok(new LessonDto
        {
            Id            = lesson.Id,
            CourseId      = lesson.CourseId,
            Title         = lesson.Title,
            Content       = lesson.Content,
            VideoUrl      = lesson.VideoUrl,
            Order         = lesson.Order,
            IsPublished   = lesson.IsPublished,
            QuestionCount = questionCount
        });
    }

    // DELETE api/lessons/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await db.Lessons.FindAsync(id);
        if (lesson is null) return NotFound();

        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync();
        return NoContent();
    }
}