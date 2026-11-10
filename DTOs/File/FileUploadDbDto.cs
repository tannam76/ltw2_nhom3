using Microsoft.AspNetCore.Http;

namespace CourseManagement.DTOs.File;

public class FileUploadDbDto
{
    /// <summary>Upload một file duy nhất</summary>
    public class UploadFileDto
    {
        public required IFormFile File { get; set; }
    }

    /// <summary>Upload nhiều file cùng lúc (batch)</summary>
    public class UploadFilesDto
    {
        public required IReadOnlyCollection<IFormFile> Files { get; set; }
    }

    public class FileResponseDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class FileDownloadDto
    {
        public required byte[] Content { get; set; }
        public required string ContentType { get; set; }
        public required string FileName { get; set; }
    }
}
