using AutoMapper;
using Domain.Abstractions.Repositories;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos.CompensationRequest;
using Domain.Models.Enums;
using Domain.Models.Result;
using Infrastructure.DataBase;
using Microsoft.EntityFrameworkCore;
namespace Api.Services;

public class CompensationRequestService(
    ICompensationRequestRepository compensationRequestRepository,
    IMapper mapper,
    IFileStorageService fileStorageService,
    ILogger<CompensationRequestService> logger)
    : ICompensationRequestService
{
    public async Task<Result<int>> CreateCompensationRequest(CompensationRequestDto compensationRequest, int userId)
    {
        logger.LogDebug("Создание запроса на компенсацию. UserId: {UserId}, BookingId: {BookingId}", 
            userId, compensationRequest.BookingId);

        try
        {
            var compensationRequestEntity = mapper.Map<CompensationRequest>(compensationRequest);
            compensationRequestEntity.TenantId = userId;

            foreach (var image in compensationRequest.ProofPhotos)
            {
                var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                logger.LogDebug("Загрузка фото для доказательства. FileName: {FileName}", fileName);
                compensationRequestEntity.ProofPhotosFileNames.Add(fileName);
                await fileStorageService.UploadFileAsync(fileName, image.Stream, image.ContentType);
            }

            await compensationRequestRepository.AddAsync(compensationRequestEntity);
            await compensationRequestRepository.SaveChangesAsync();

            logger.LogInformation("Запрос на компенсацию создан. RequestId: {RequestId}", compensationRequestEntity.Id);
            return Result<int>.Success(SuccessType.Created, compensationRequestEntity.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании запроса на компенсацию. UserId: {UserId}", userId);
            return Result<int>.Failure(new Error(ex.Message, ErrorType.ServerError));
        }
    }

    public async Task<Result> DeleteCompensationRequest(int compensationRequestId)
    {
        logger.LogDebug("Удаление запроса на компенсацию. RequestId: {RequestId}", compensationRequestId);

        try
        {
            var deletedCount = await compensationRequestRepository.DeleteByIdAsync(compensationRequestId);

            if (deletedCount == 0)
            {
                logger.LogWarning("Запрос на компенсацию не найден при удалении. RequestId: {RequestId}", compensationRequestId);
                return Result.Failure(new Error("Compensation request not found", ErrorType.NotFound));
            }

            logger.LogInformation("Запрос на компенсацию удален. RequestId: {RequestId}", compensationRequestId);
            return Result.Success(SuccessType.NoContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении запроса на компенсацию. RequestId: {RequestId}", compensationRequestId);
            return Result.Failure(new Error(ex.Message, ErrorType.ServerError));
        }
    }

    public async Task<Result<CompensationRequestResponse>> GetCompensationRequestByBookingId(int bookingId)
    {
        logger.LogDebug("Получение запроса на компенсацию по BookingId: {BookingId}", bookingId);

        var compensationRequest = await compensationRequestRepository.GetByBookingIdAsync(bookingId);

        if (compensationRequest == null)
        {
            logger.LogWarning("Запрос на компенсацию не найден. BookingId: {BookingId}", bookingId);
            return Result<CompensationRequestResponse>.Failure(
                new Error("There is no compensation request with booking id: " + bookingId, ErrorType.NotFound));
        }

        logger.LogDebug("Запрос на компенсацию получен. RequestId: {RequestId}", compensationRequest.Id);
        return Result<CompensationRequestResponse>.Success(
            SuccessType.Ok, 
            mapper.Map<CompensationRequestResponse>(compensationRequest));
    }

    public async Task<Result> RejectCompensationRequest(int compensationRequestId, int userId)
    {
        logger.LogDebug("Отклонение запроса на компенсацию. RequestId: {RequestId}, UserId: {UserId}", 
            compensationRequestId, userId);

        var compensationRequest = await compensationRequestRepository
            .GetWithBookingAndPropertyAsync(compensationRequestId);

        if (compensationRequest == null)
        {
            logger.LogWarning("Запрос на компенсацию не найден при отклонении. RequestId: {RequestId}", compensationRequestId);
            return Result.Failure(new Error("Compensation request not found.", ErrorType.NotFound));
        }

        if (compensationRequest.Booking.Property.OwnerId != userId)
        {
            logger.LogWarning("Попытка отклонения запроса не владельцем. UserId: {UserId}, PropertyId: {PropertyId}", 
                userId, compensationRequest.Booking.Property.Id);
            return Result.Failure(
                new Error("Cannot reject compensation request if you are not an owner of property", ErrorType.BadRequest));
        }

        compensationRequest.Status = CompensationStatus.Rejected;
        await compensationRequestRepository.SaveChangesAsync();

        logger.LogInformation("Запрос на компенсацию отклонен. RequestId: {RequestId}", compensationRequestId);
        return Result.Success(SuccessType.NoContent);
    }

    public async Task<Result> ApproveCompensationRequest(int userId, int compensationRequestId, decimal amount)
    {
        logger.LogDebug("Подтверждение запроса на компенсацию. RequestId: {RequestId}, UserId: {UserId}, Amount: {Amount}", 
            compensationRequestId, userId, amount);

        var compensationRequest = await compensationRequestRepository
            .GetWithBookingPropertyAndTenantAsync(compensationRequestId);

        if (compensationRequest == null)
        {
            logger.LogWarning("Запрос на компенсацию не найден при подтверждении. RequestId: {RequestId}", compensationRequestId);
            return Result.Failure(
                new Error("Compensation request not found.", ErrorType.NotFound));
        }

        if (compensationRequest.Booking.Property.OwnerId != userId)
        {
            logger.LogWarning("Попытка подтверждения запроса не владельцем. UserId: {UserId}, PropertyId: {PropertyId}", 
                userId, compensationRequest.Booking.Property.Id);
            return Result.Failure(
                new Error("Cannot approve compensation request if you are not an owner of property", ErrorType.BadRequest));
        }

        if (amount > compensationRequest.Booking.TotalPrice)
        {
            logger.LogWarning("Сумма компенсации превышает стоимость бронирования. Amount: {Amount}, TotalPrice: {TotalPrice}", 
                amount, compensationRequest.Booking.TotalPrice);
            return Result.Failure(
                new Error("Cannot approve amount more than booking total price", ErrorType.BadRequest));
        }

        compensationRequest.Tenant.Balance += amount;
        compensationRequest.Status = CompensationStatus.Approved;
        await compensationRequestRepository.SaveChangesAsync();

        logger.LogInformation(
            "Запрос на компенсацию подтвержден. RequestId: {RequestId}, ApprovedAmount: {Amount}", 
            compensationRequestId, amount);
        return Result.Success(SuccessType.NoContent);
    }

    public async Task<Result<CompensationRequestResponse>> GetCompensationRequestById(int requestId)
    {
        logger.LogDebug("Получение запроса на компенсацию по Id: {RequestId}", requestId);

        var compensationRequest = await compensationRequestRepository.GetByFilterAsync(r => r.Id == requestId);

        if (compensationRequest == null)
        {
            logger.LogWarning("Запрос на компенсацию не найден.");
            return Result<CompensationRequestResponse>.Failure(
                new Error("There is no compensation request", ErrorType.NotFound));
        }

        logger.LogDebug("Запрос на компенсацию получен. RequestId: {RequestId}", compensationRequest.Id);
        return Result<CompensationRequestResponse>.Success(
            SuccessType.Ok, 
            mapper.Map<CompensationRequestResponse>(compensationRequest));
    }
}