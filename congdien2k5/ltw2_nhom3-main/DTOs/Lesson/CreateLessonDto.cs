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
}
