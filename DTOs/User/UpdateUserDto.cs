using System.ComponentModel.DataAnnotations;

namespace CourseManagement.DTOs.User;

public class UpdateUserDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Role { get; set; }
}
