namespace CourseManagement.DTOs.Common;

public class UserQueryParameters
{
    private const int MaxPageSize = 50;

    private int _pageNumber = 1;
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = Math.Max(1, value);
    }

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public string? SortBy { get; set; }
    public bool IsDescending { get; set; }
}
