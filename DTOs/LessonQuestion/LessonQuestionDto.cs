using CourseManagement.Models;

namespace CourseManagement.DTOs.LessonQuestion;

// ── Response DTOs ──────────────────────────────────────────────────────────────

public class LessonQuestionDto
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public int CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;   // "Essay" | "MultipleChoice"
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AnswerCount { get; set; }
    public IReadOnlyList<LessonQuestionOptionDto> Options { get; set; } = [];
}

public class LessonQuestionOptionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    // IsCorrect chỉ trả về cho instructor
    public bool? IsCorrect { get; set; }
}

public class LessonQuestionAnswerDto
{
    public int Id { get; set; }
    public int LessonQuestionId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? EssayText { get; set; }
    public int? SelectedOptionIndex { get; set; }
    public bool? IsCorrect { get; set; }          // null nếu tự luận chưa chấm
    public int? Score { get; set; }
    public string? InstructorFeedback { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
}
