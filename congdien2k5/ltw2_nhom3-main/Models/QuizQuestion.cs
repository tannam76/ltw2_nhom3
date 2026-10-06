namespace CourseManagement.Models;

public class QuizQuestion
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public string Question { get; set; } = string.Empty;
    public int CorrectOptionIndex { get; set; }
    public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
}
