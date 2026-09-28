namespace CourseManagement.Models;

public class Quiz
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public int PassingScore { get; set; } = 50;
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
}
