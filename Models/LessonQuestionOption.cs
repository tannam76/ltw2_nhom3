namespace CourseManagement.Models;

/// <summary>Đáp án cho câu trắc nghiệm</summary>
public class LessonQuestionOption
{
    public int Id { get; set; }
    public int LessonQuestionId { get; set; }
    public LessonQuestion LessonQuestion { get; set; } = null!;

    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}
