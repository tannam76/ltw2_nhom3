using System.ComponentModel.DataAnnotations;
using CourseManagement.Models;

namespace CourseManagement.DTOs.LessonQuestion;

// ── Request DTOs ───────────────────────────────────────────────────────────────

/// <summary>Tạo câu hỏi mới cho bài học</summary>
public class CreateLessonQuestionDto
{
    [Required]
    public int LessonId { get; set; }

    /// <summary>ID của người tạo (instructor)</summary>
    [Required]
    public int CreatorId { get; set; }

    [Required, StringLength(2000)]
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>"Essay" hoặc "MultipleChoice"</summary>
    [Required]
    public string Type { get; set; } = "Essay";

    public int Order { get; set; }

    /// <summary>Đáp án (bắt buộc nếu Type = MultipleChoice, ít nhất 2 lựa chọn)</summary>
    public List<CreateLessonQuestionOptionDto> Options { get; set; } = [];
}

public class CreateLessonQuestionOptionDto
{
    [Required, StringLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}

/// <summary>Cập nhật câu hỏi</summary>
public class UpdateLessonQuestionDto
{
    [Required, StringLength(2000)]
    public string QuestionText { get; set; } = string.Empty;

    public int Order { get; set; }

    /// <summary>Chỉ dùng khi Type = MultipleChoice</summary>
    public List<CreateLessonQuestionOptionDto> Options { get; set; } = [];
}

/// <summary>Học sinh nộp câu trả lời</summary>
public class SubmitAnswerDto
{
    [Required]
    public int UserId { get; set; }

    /// <summary>Tự luận: nội dung bài viết</summary>
    [StringLength(10000)]
    public string? EssayText { get; set; }

    /// <summary>Trắc nghiệm: index đáp án chọn (0-based)</summary>
    public int? SelectedOptionIndex { get; set; }
}

/// <summary>Instructor chấm điểm câu tự luận</summary>
public class GradeAnswerDto
{
    [Range(0, 100)]
    public int Score { get; set; }

    [StringLength(2000)]
    public string? Feedback { get; set; }
}
