using System.ComponentModel.DataAnnotations;

namespace CourseManagement.DTOs.Product;

public class UpdateProductDto
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Url]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }
}