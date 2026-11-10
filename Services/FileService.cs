using CourseManagement.Models;
using CourseManagement.Repositories;
using Microsoft.AspNetCore.Http;
using static CourseManagement.DTOs.File.FileUploadDbDto;
using System;
using System.IO;
namespace CourseManagement.Services;

public class FileService : IFileService
{
    // Giới hạn kích thước mỗi file: 1 MB
    private const long MaxFileSizeBytes = 1 * 1024 * 1024;

    // Giới hạn tổng dung lượng batch upload: 25 MB
    private const long MaxBatchSizeBytes = 25 * 1024 * 1024;

    // Extension → MIME type được chấp nhận
    private static readonly IReadOnlyDictionary<string, string> AllowedFileTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"]  = "application/pdf",
            [".png"]  = "image/png",
            [".jpg"]  = "image/jpeg",
            [".jpeg"] = "image/jpeg"
        };

    private readonly IFileRepository _fileRepository;

    public FileService(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    // ─── Upload một file ────────────────────────────────────────────────────────

    public async Task<FileResponseDto> UploadFileAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // Kiểm tra trùng tên file
    var existing = await _fileRepository.GetByFileNameAsync(Path.GetFileName(file.FileName), cancellationToken);
    if (existing != null)
        throw new ArgumentException($"File '{file.FileName}' đã được tải lên trước đó và không thể tải lại.");

    var storedFile = await ToStoredFileAsync(file, cancellationToken);
        var createdFile = await _fileRepository.AddAsync(storedFile, cancellationToken);
        return ToResponse(createdFile);
    }

    // ─── Upload nhiều file (batch) ───────────────────────────────────────────────

    public async Task<IReadOnlyList<FileResponseDto>> UploadFilesAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
            throw new ArgumentException("Chọn ít nhất một file.", nameof(files));

        // Kiểm tra metadata + tổng dung lượng trước khi đọc nội dung
        long totalSize = 0;
        foreach (var file in files)
        {
            ValidateFileMetadata(file);
            totalSize = checked(totalSize + file.Length);
            if (totalSize > MaxBatchSizeBytes)
                throw new ArgumentException(
                    $"Tổng dung lượng không được vượt quá {MaxBatchSizeBytes / (1024 * 1024)} MB.",
                    nameof(files));
        }

        var storedFiles = new List<StoredFile>(files.Count);
        foreach (var file in files)
        {
            // Kiểm tra trùng tên trong batch
            var existingBatch = await _fileRepository.GetByFileNameAsync(Path.GetFileName(file.FileName), cancellationToken);
            if (existingBatch != null)
                throw new ArgumentException($"File '{file.FileName}' đã được tải lên trước đó và không thể tải lại.");

            storedFiles.Add(await ToStoredFileAsync(file, cancellationToken));
        }

        var createdFiles = await _fileRepository.AddRangeAsync(storedFiles, cancellationToken);
        return createdFiles.Select(ToResponse).ToList();
    }

    // ─── Lấy file để tải về ─────────────────────────────────────────────────────

    public async Task<FileDownloadDto?> GetFileForDownloadAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(id, cancellationToken);
        if (file is null) return null;

        return new FileDownloadDto
        {
            Content     = file.Content,
            ContentType = file.ContentType,
            FileName    = file.FileName
        };
    }

    // ─── Private helpers ─────────────────────────────────────────────────────────

    /// <summary>Kiểm tra metadata: kích thước, extension, MIME type.</summary>
    private static string ValidateFileMetadata(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("File không hợp lệ hoặc rỗng.");

        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException(
                $"File '{file.FileName}' vượt quá giới hạn {MaxFileSizeBytes / (1024 * 1024)} MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedFileTypes.TryGetValue(extension, out var expectedContentType))
        {
            throw new ArgumentException(
                $"File '{file.FileName}' không được hỗ trợ. Chỉ chấp nhận: {string.Join(", ", AllowedFileTypes.Keys)}.");
        }

        if (!string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Kiểu MIME của file '{file.FileName}' không khớp với phần mở rộng '{extension}'.");

        return expectedContentType;
    }

    /// <summary>Đọc stream file, kiểm tra magic bytes và tạo StoredFile.</summary>
    private static async Task<StoredFile> ToStoredFileAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var contentType = ValidateFileMetadata(file);

        await using var memoryStream = new MemoryStream();
        await using var inputStream  = file.OpenReadStream();

        var buffer = new byte[81920]; // 80 KB buffer
        int bytesRead;
        while ((bytesRead = await inputStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memoryStream.Length + bytesRead > MaxFileSizeBytes)
                throw new ArgumentException(
                    $"File '{file.FileName}' vượt quá giới hạn {MaxFileSizeBytes / (1024 * 1024)} MB.");

            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        // Kiểm tra magic bytes (chữ ký nhị phân của file)
        ValidateFileSignature(
            file.FileName,
            contentType,
            memoryStream.GetBuffer().AsSpan(0, (int)memoryStream.Length));

        return new StoredFile
        {
            FileName    = Path.GetFileName(file.FileName.Replace('\\', '/')),
            ContentType = contentType,
            Content     = memoryStream.ToArray(),
            Length      = memoryStream.Length
        };
    }

    /// <summary>Kiểm tra magic bytes để đảm bảo nội dung file thực sự khớp với loại file.</summary>
    private static void ValidateFileSignature(
        string fileName,
        string contentType,
        ReadOnlySpan<byte> content)
    {
        var isValid = contentType switch
        {
            "application/pdf" => content.StartsWith("%PDF-"u8),
            "image/png"       => content.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/jpeg"      => content.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            _                 => false
        };

        if (!isValid)
            throw new ArgumentException(
                $"Nội dung file '{fileName}' không khớp với kiểu file được khai báo (magic bytes không hợp lệ).");
    }

    private static FileResponseDto ToResponse(StoredFile file) => new()
    {
        Id         = file.Id,
        FileName   = file.FileName,
        Size       = file.Length,
        UploadedAt = file.UploadedAt
    };
}
