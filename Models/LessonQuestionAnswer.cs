namespace CourseManagement.Models;

/// <summary>Câu trả lời của học sinh cho câu hỏi hỏi đáp</summary>
public class LessonQuestionAnswer
{
    public int Id { get; set; }
    public int LessonQuestionId { get; set; }
    public LessonQuestion LessonQuestion { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Tự luận: lưu nội dung bài viết
    public string? EssayText { get; set; }

    // Trắc nghiệm: lưu index đáp án chọn (0-based)
    public int? SelectedOptionIndex { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Chấm điểm thủ công cho tự luận (null = chưa chấm)
    public int? Score { get; set; }          // 0–100
    public string? InstructorFeedback { get; set; }
    public DateTime? GradedAt { get; set; }
}
