namespace CourseManagement.Models;

public class QuizOption
{
    public int Id { get; set; }
    public int QuizQuestionId { get; set; }
    public QuizQuestion QuizQuestion { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
}
