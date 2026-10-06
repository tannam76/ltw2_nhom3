namespace CourseManagement.Models;

public class Lesson
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? VideoUrl { get; set; }
    public int Order { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
