using Domain.Models.Result;

namespace Domain.Abstractions.Services;

public interface IFileStorageService
{
    Task<Result> UploadFileAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
    Task<Result<Stream>> DownloadFileAsync(string fileName, CancellationToken cancellationToken = default);
    Task<Result<string>> DownloadFileAsBase64Async(string fileName, CancellationToken cancellationToken = default);
    Task<Result> DeleteFileAsync(string fileName, CancellationToken cancellationToken = default);
    Task<Result<bool>> FileExistsAsync(string fileName, CancellationToken cancellationToken = default);
    Task<Result<string>> GetFileUrlAsync(string fileName, CancellationToken cancellationToken = default);
}