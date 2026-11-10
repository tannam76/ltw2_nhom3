using CourseManagement.Data;
using CourseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController(AppDbContext db) : ControllerBase
{
    // GET api/enrollments?userId=1
    [HttpGet]
    public async Task<IActionResult> GetByUser([FromQuery] int userId)
    {
        if (userId <= 0)
            return BadRequest(new { message = "userId is required." });

        var enrollments = await db.Enrollments
            .Include(e => e.Course).ThenInclude(c => c.Instructor)
            .Include(e => e.Course).ThenInclude(c => c.Lessons)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new
            {
                e.Id,
                e.UserId,
                e.CourseId,
                e.EnrolledAt,
                Course = new
                {
                    e.Course.Id,
                    e.Course.Title,
                    e.Course.Description,
                    e.Course.ThumbnailUrl,
                    e.Course.IsPublished,
                    InstructorName = e.Course.Instructor.FullName,
                    LessonCount    = e.Course.Lessons.Count
                }
            })
            .ToListAsync();

        return Ok(enrollments);
    }

    // GET api/enrollments/course/{courseId} — Danh sách học viên của khóa học
    [HttpGet("course/{courseId:int}")]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var enrollments = await db.Enrollments
            .Include(e => e.User)
            .Where(e => e.CourseId == courseId)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new
            {
                e.Id,
                e.UserId,
                e.CourseId,
                e.EnrolledAt,
                User = new { e.User.Id, e.User.FullName, e.User.Email, e.User.Role }
            })
            .ToListAsync();

        return Ok(enrollments);
    }

    // GET api/enrollments/check?userId=1&courseId=2
    [HttpGet("check")]
    public async Task<IActionResult> Check([FromQuery] int userId, [FromQuery] int courseId)
    {
        var enrolled = await db.Enrollments
            .AnyAsync(e => e.UserId == userId && e.CourseId == courseId);
        return Ok(new { enrolled });
    }

    // POST api/enrollments — Đăng ký khóa học
    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request)
    {
        var userExists   = await db.Users.AnyAsync(u => u.Id == request.UserId);
        var courseExists = await db.Courses.AnyAsync(c => c.Id == request.CourseId && c.IsPublished);

        if (!userExists)   return BadRequest(new { message = "Người dùng không tồn tại." });
        if (!courseExists) return BadRequest(new { message = "Khóa học không tồn tại hoặc chưa được xuất bản." });

        var alreadyEnrolled = await db.Enrollments
            .AnyAsync(e => e.UserId == request.UserId && e.CourseId == request.CourseId);
        if (alreadyEnrolled)
            return Conflict(new { message = "Người dùng đã đăng ký khóa học này." });

        var enrollment = new Enrollment
        {
            UserId     = request.UserId,
            CourseId   = request.CourseId,
            EnrolledAt = DateTime.UtcNow
        };

        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        return Ok(new { enrollment.Id, enrollment.UserId, enrollment.CourseId, enrollment.EnrolledAt });
    }

    // DELETE api/enrollments/{id} — Hủy đăng ký
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Unenroll(int id)
    {
        var enrollment = await db.Enrollments.FindAsync(id);
        if (enrollment is null) return NotFound();

        db.Enrollments.Remove(enrollment);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record EnrollRequest(int UserId, int CourseId);