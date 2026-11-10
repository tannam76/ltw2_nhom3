using System.ComponentModel.DataAnnotations;

namespace CourseManagement.DTOs.Lesson;

/// <summary>Yêu cầu tự động sinh câu hỏi từ nội dung bài học</summary>
public class AutoGenerateQuestionsRequestDto
{
    /// <summary>Số lượng câu hỏi muốn tự động sinh (mặc định 3, tối đa 10)</summary>
    [Range(1, 10)]
    public int Count { get; set; } = 3;

    /// <summary>Loại câu hỏi: "Both" (cả hai), "Essay" (tự luận), hoặc "MultipleChoice" (trắc nghiệm)</summary>
    public string Type { get; set; } = "Both";

    /// <summary>True = lưu trực tiếp vào cơ sở dữ liệu của bài học, False = chỉ xem trước (preview)</summary>
    public bool SaveToLesson { get; set; } = true;

    /// <summary>ID người tạo câu hỏi (nếu để trống, hệ thống sẽ tự lấy giảng viên của khóa học)</summary>
    public int? CreatorId { get; set; }
}

/// <summary>Yêu cầu xem trước câu hỏi gợi ý từ nội dung đang nhập (chưa cần lưu bài học)</summary>
public class PreviewQuestionsRequestDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Count { get; set; } = 3;

    public string Type { get; set; } = "Both";
}
