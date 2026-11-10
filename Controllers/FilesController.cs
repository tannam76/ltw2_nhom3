using CourseManagement.Services;
using Microsoft.AspNetCore.Mvc;
using static CourseManagement.DTOs.File.FileUploadDbDto;

namespace CourseManagement.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    // POST: api/files/upload - Upload một file
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FileResponseDto>> Upload(
        [FromForm] UploadFileDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fileService.UploadFileAsync(dto.File, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST: api/files/upload-batch - Upload nhiều file cùng lúc
    [HttpPost("upload-batch")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<IReadOnlyList<FileResponseDto>>> UploadBatch(
        [FromForm] UploadFilesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await _fileService.UploadFilesAsync(dto.Files, cancellationToken);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: api/files/{id}/download - Tải file về máy (Content-Disposition: attachment)
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var fileDto = await _fileService.GetFileForDownloadAsync(id, cancellationToken);
        if (fileDto is null)
            return NotFound(new { message = $"Không tìm thấy file với Id = {id}." });

        return File(fileDto.Content, fileDto.ContentType, fileDto.FileName);
    }

    // GET: api/files/{id}/view - Xem file trực tiếp trong trình duyệt (inline)
    [HttpGet("{id:int}/view")]
    public async Task<IActionResult> ViewFile(int id, CancellationToken cancellationToken)
    {
        var fileDto = await _fileService.GetFileForDownloadAsync(id, cancellationToken);
        if (fileDto is null)
            return NotFound(new { message = $"Không tìm thấy file với Id = {id}." });

        return File(fileDto.Content, fileDto.ContentType);
    }
}
