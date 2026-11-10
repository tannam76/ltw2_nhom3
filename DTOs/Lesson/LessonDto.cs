using CourseManagement.DTOs.LessonQuestion;

namespace CourseManagement.DTOs.Lesson;

public class LessonDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? VideoUrl { get; set; }
    public int Order { get; set; }
    public bool IsPublished { get; set; }

    /// <summary>Số lượng câu hỏi hỏi đáp hiện có của bài học</summary>
    public int QuestionCount { get; set; }

    /// <summary>Danh sách câu hỏi đính kèm (nếu có)</summary>
    public List<LessonQuestionDto>? Questions { get; set; }
}
