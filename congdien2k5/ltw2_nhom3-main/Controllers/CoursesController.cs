using CourseManagement.Data;
using CourseManagement.DTOs.Course;
using CourseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController(AppDbContext db) : ControllerBase
{
    // GET api/courses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetAll()
    {
        var courses = await db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Lessons)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => ToDto(c))
            .ToListAsync();

        return Ok(courses);
    }

    // GET api/courses/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDto>> GetById(int id)
    {
        var course = await db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id);

        return course is null ? NotFound() : Ok(ToDto(course));
    }

    // POST api/courses
    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseDto dto)
    {
        var instructorExists = await db.Users.AnyAsync(u => u.Id == dto.InstructorId);
        if (!instructorExists)
            return BadRequest(new { message = "Instructor not found." });

        var course = new Course
        {
            Title       = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            ThumbnailUrl = dto.ThumbnailUrl,
            InstructorId = dto.InstructorId,
            IsPublished  = false,
            CreatedAt    = DateTime.UtcNow
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync();

        await db.Entry(course).Reference(c => c.Instructor).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, ToDto(course));
    }

    // PUT api/courses/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CourseDto>> Update(int id, UpdateCourseDto dto)
    {
        var course = await db.Courses
            .Include(c => c.Instructor)
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course is null) return NotFound();

        course.Title        = dto.Title.Trim();
        course.Description  = dto.Description.Trim();
        course.ThumbnailUrl = dto.ThumbnailUrl;
        course.IsPublished  = dto.IsPublished;

        await db.SaveChangesAsync();
        return Ok(ToDto(course));
    }

    // PATCH api/courses/{id}/publish
    [HttpPatch("{id:int}/publish")]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        course.IsPublished = !course.IsPublished;
        await db.SaveChangesAsync();
        return Ok(new { isPublished = course.IsPublished });
    }

    // DELETE api/courses/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        db.Courses.Remove(course);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static CourseDto ToDto(Course c) => new()
    {
        Id             = c.Id,
        Title          = c.Title,
        Description    = c.Description,
        ThumbnailUrl   = c.ThumbnailUrl,
        InstructorId   = c.InstructorId,
        InstructorName = c.Instructor?.FullName ?? string.Empty,
        IsPublished    = c.IsPublished,
        CreatedAt      = c.CreatedAt,
        LessonCount    = c.Lessons?.Count ?? 0
    };
}