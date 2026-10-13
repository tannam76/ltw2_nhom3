using System.ComponentModel.DataAnnotations;

namespace CourseManagement.DTOs.Lesson;

public class CreateLessonDto
{
    [Required]
    public int CourseId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    [Url]
    public string? VideoUrl { get; set; }

    [Range(0, int.MaxValue)]
    public int Order { get; set; }

    /// <summary>
    /// Đặt = true nếu người tạo bài học muốn tự động sinh câu hỏi (hỏi đáp) dựa trên nội dung bài học đưa vào
    /// </summary>
    public bool AutoGenerateQuestions { get; set; } = false;

    /// <summary>Số lượng câu hỏi muốn tự sinh (1 - 10, mặc định 3)</summary>
    [Range(1, 10)]
    public int AutoQuestionCount { get; set; } = 3;

    /// <summary>Loại câu hỏi muốn sinh: "Both", "Essay", "MultipleChoice"</summary>
    public string AutoQuestionType { get; set; } = "Both";

    /// <summary>ID của người tạo (nếu để trống, tự lấy Instructor của Course)</summary>
    public int? CreatorId { get; set; }
}
