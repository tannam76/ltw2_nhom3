using CourseManagement.Data;
using CourseManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProgressController(AppDbContext db) : ControllerBase
{
    // GET api/progress?userId=1&courseId=2 — Tiến độ học tập của user trong 1 khóa
    [HttpGet]
    public async Task<IActionResult> GetProgress([FromQuery] int userId, [FromQuery] int courseId)
    {
        if (userId <= 0 || courseId <= 0)
            return BadRequest(new { message = "userId và courseId là bắt buộc." });

        var lessons = await db.Lessons
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.Order)
            .ToListAsync();

        var completedIds = await db.LessonProgresses
            .Where(lp => lp.UserId == userId && lp.IsCompleted &&
                         lessons.Select(l => l.Id).Contains(lp.LessonId))
            .Select(lp => lp.LessonId)
            .ToListAsync();

        int total     = lessons.Count;
        int completed = completedIds.Count;
        int percent   = total == 0 ? 0 : (int)Math.Round((double)completed / total * 100);

        var details = lessons.Select(l => new
        {
            l.Id,
            l.Title,
            l.Order,
            IsCompleted = completedIds.Contains(l.Id)
        }).ToList();

        return Ok(new { userId, courseId, total, completed, percent, lessons = details });
    }

    // POST api/progress/complete — Đánh dấu bài học hoàn thành
    [HttpPost("complete")]
    public async Task<IActionResult> MarkComplete([FromBody] ProgressRequest request)
    {
        var userExists   = await db.Users.AnyAsync(u => u.Id == request.UserId);
        var lessonExists = await db.Lessons.AnyAsync(l => l.Id == request.LessonId);

        if (!userExists)   return BadRequest(new { message = "Người dùng không tồn tại." });
        if (!lessonExists) return BadRequest(new { message = "Bài học không tồn tại." });

        var existing = await db.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.UserId == request.UserId && lp.LessonId == request.LessonId);

        if (existing is not null)
        {
            existing.IsCompleted = true;
            existing.UpdatedAt   = DateTime.UtcNow;
        }
        else
        {
            db.LessonProgresses.Add(new LessonProgress
            {
                UserId      = request.UserId,
                LessonId    = request.LessonId,
                IsCompleted = true,
                UpdatedAt   = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return Ok(new { message = "Đánh dấu hoàn thành thành công." });
    }

    // POST api/progress/incomplete — Đánh dấu bài học chưa hoàn thành
    [HttpPost("incomplete")]
    public async Task<IActionResult> MarkIncomplete([FromBody] ProgressRequest request)
    {
        var existing = await db.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.UserId == request.UserId && lp.LessonId == request.LessonId);

        if (existing is null) return NotFound(new { message = "Chưa có dữ liệu tiến độ." });

        existing.IsCompleted = false;
        existing.UpdatedAt   = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Đã cập nhật trạng thái." });
    }
}

public record ProgressRequest(int UserId, int LessonId);