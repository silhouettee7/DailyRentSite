using Domain.Abstractions.Services;
using Domain.Models.Result;
using Infrastructure.FileStorage.Options;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Infrastructure.FileStorage;

public class MinioService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioOptions _minioOptions;

    public MinioService(IOptionsMonitor<MinioOptions> minioOptions)
    {
        _minioOptions = minioOptions.CurrentValue;
        _minioClient = new MinioClient()
            .WithEndpoint(_minioOptions.Endpoint)
            .WithCredentials(_minioOptions.AccessKey, _minioOptions.SecretKey)
            .WithSSL(false)
            .Build();
    }

    public async Task<Result> UploadFileAsync(string fileName, Stream fileStream, string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bucketExists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_minioOptions.BucketName), cancellationToken);
            if (!bucketExists)
            {
                await _minioClient
                    .MakeBucketAsync(new MakeBucketArgs()
                        .WithBucket(_minioOptions.BucketName), cancellationToken);
            }

            fileStream.Position = 0;
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType), cancellationToken);

            return Result.Success(SuccessType.Created);
        }
        catch
        {
            return Result.Failure(new Error( $"Can not upload file {fileName}",ErrorType.ServerError));
        }
    }

    public async Task<Result<Stream>> DownloadFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var existResult = await FileExistsAsync(fileName, cancellationToken);
            if (!existResult.IsSuccess)
            {
                return Result<Stream>.Failure(existResult?.Error ?? new Error( $"Can not download file {fileName}",ErrorType.ServerError));
            }

            if (!existResult.Value)
            {
                return Result<Stream>.Failure(new Error( "File not found",ErrorType.NotFound));
            }

            var memoryStream = new MemoryStream();
            await _minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_minioOptions.BucketName)
                    .WithObject(fileName)
                    .WithCallbackStream(stream => stream.CopyTo(memoryStream)), cancellationToken);

            memoryStream.Seek(0, SeekOrigin.Begin);
            return Result<Stream>.Success(SuccessType.Ok, memoryStream);
        }
        catch
        {
            return Result<Stream>.Failure(new Error( $"Can not download file {fileName}",ErrorType.ServerError));
        }
    }

    public async Task<Result<string>> DownloadFileAsBase64Async(string fileName, CancellationToken cancellationToken = default)
    {
        var downloadResult = await DownloadFileAsync(fileName, cancellationToken);
        if (!downloadResult.IsSuccess)
        {
            return Result<string>.Failure(downloadResult.Error!);
        }
        var bytes = ((MemoryStream)downloadResult.Value!).ToArray();
        
        return Result<string>.Success(SuccessType.Ok, Convert.ToBase64String(bytes));
    }

    public async Task<Result> DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var existResult = await FileExistsAsync(fileName, cancellationToken);
            if (!existResult.IsSuccess)
            {
                return Result<Stream>.Failure(existResult?.Error ?? new Error( $"Can not delete file {fileName}",ErrorType.ServerError));
            }

            if (!existResult.Value)
            {
                return Result<Stream>.Failure(new Error( "File not found",ErrorType.NotFound));
            }
            await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(fileName), cancellationToken);

            return Result.Success(SuccessType.NoContent);
        }
        catch (Exception)
        {
            return Result.Failure(new Error( "Could not delete file",ErrorType.ServerError));
        }
    }

    public async Task<Result<bool>> FileExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(fileName), cancellationToken);

            return Result<bool>.Success(SuccessType.Ok,true);
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return Result<bool>.Success(SuccessType.Ok,false);
        }
        catch (Exception)
        {
            return Result<bool>.Failure(new Error( "File not found",ErrorType.ServerError));
        }
    }

    public async Task<Result<string>> GetFileUrlAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiryInSeconds = 60;

            var args = new PresignedGetObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(fileName)
                .WithExpiry(expiryInSeconds);

            var url = await _minioClient.PresignedGetObjectAsync(args);

            return Result<string>.Success(SuccessType.Ok,url);
        }
        catch (Exception)
        {
            return Result<string>.Failure(new Error( "Could not get file url",ErrorType.ServerError));
        }
    }
}