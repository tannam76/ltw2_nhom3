using Microsoft.AspNetCore.Http;
using static CourseManagement.DTOs.File.FileUploadDbDto;

namespace CourseManagement.Services;

public interface IFileService
{
    Task<FileResponseDto> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileResponseDto>> UploadFilesAsync(IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken = default);
    Task<FileDownloadDto?> GetFileForDownloadAsync(int id, CancellationToken cancellationToken = default);
}
