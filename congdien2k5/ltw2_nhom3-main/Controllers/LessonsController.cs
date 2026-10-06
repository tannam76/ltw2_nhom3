using CourseManagement.Data;
using CourseManagement.DTOs.Lesson;
using CourseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonsController(AppDbContext db) : ControllerBase
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
            .Select(l => ToDto(l))
            .ToListAsync();

        return Ok(lessons);
    }

    // GET api/lessons/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LessonDto>> GetById(int id)
    {
        var lesson = await db.Lessons.FindAsync(id);
        return lesson is null ? NotFound() : Ok(ToDto(lesson));
    }

    // POST api/lessons
    [HttpPost]
    public async Task<ActionResult<LessonDto>> Create(CreateLessonDto dto)
    {
        var courseExists = await db.Courses.AnyAsync(c => c.Id == dto.CourseId);
        if (!courseExists)
            return BadRequest(new { message = "Course not found." });

        // Auto-assign next order if not specified
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
        return CreatedAtAction(nameof(GetById), new { id = lesson.Id }, ToDto(lesson));
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
        return Ok(ToDto(lesson));
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

    private static LessonDto ToDto(Lesson l) => new()
    {
        Id          = l.Id,
        CourseId    = l.CourseId,
        Title       = l.Title,
        Content     = l.Content,
        VideoUrl    = l.VideoUrl,
        Order       = l.Order,
        IsPublished = l.IsPublished
    };
}