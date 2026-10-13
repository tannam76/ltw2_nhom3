namespace CourseManagement.Models;

/// <summary>Loại câu hỏi: Tự luận hoặc Trắc nghiệm</summary>
public enum QuestionType
{
    Essay = 0,          // Tự luận
    MultipleChoice = 1  // Trắc nghiệm
}

/// <summary>Câu hỏi hỏi đáp gắn vào bài học (do instructor tạo)</summary>
public class LessonQuestion
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public int CreatorId { get; set; }
    public User Creator { get; set; } = null!;

    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.Essay;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LessonQuestionOption> Options { get; set; } = new List<LessonQuestionOption>();
    public ICollection<LessonQuestionAnswer> Answers { get; set; } = new List<LessonQuestionAnswer>();
}
