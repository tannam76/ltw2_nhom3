namespace CourseManagement.Models;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int InstructorId { get; set; }
    public User Instructor { get; set; } = null!;
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
