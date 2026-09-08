using System.ComponentModel.DataAnnotations;

namespace CourseManagement.DTOs.Course;

public class CreateCourseDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Url]
    public string? ThumbnailUrl { get; set; }

    [Required]
    public int InstructorId { get; set; }
}
