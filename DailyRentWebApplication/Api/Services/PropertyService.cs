using Api.Utils;
using AutoMapper;
using Domain.Abstractions.Repositories;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos.Image;
using Domain.Models.Dtos.Property;
using Domain.Models.Enums;
using Domain.Models.Result;
using Infrastructure.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class PropertyService(
    IPropertyRepository propertyRepository,
    IFileStorageService fileStorageService,
    FileWorker fileWorker,
    IMapper mapper,
    AppDbContext context,
    ILogger<PropertyService> logger)
    : IPropertyService
{
    public async Task<Result<int>> CreatePropertyAsync(PropertyCreateRequest propertyCreateRequest, int userId)
    {
        logger.LogDebug("Начато создание объекта недвижимости для пользователя {UserId}", userId);
        
        var property = mapper.Map<Property>(propertyCreateRequest);
        property.OwnerId = userId;

        try
        {
            var mainImage = propertyCreateRequest.MainImage;
            var mainImageFileName = $"main_{Guid.NewGuid()}_{mainImage.FileName}";
            logger.LogDebug("Загрузка главного изображения: {FileName}", mainImageFileName);

            var propertyMainImage = new PropertyImage
            {
                CreatedAt = DateTime.UtcNow,
                FileName = mainImageFileName,
                IsMain = true
            };
            property.Images.Add(propertyMainImage);

            await fileStorageService.UploadFileAsync(mainImageFileName, mainImage.Stream, mainImage.ContentType);
            
            foreach (var image in propertyCreateRequest.PropertyImages)
            {
                var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                logger.LogDebug("Загрузка дополнительного изображения: {FileName}", fileName);

                var propertyImage = new PropertyImage
                {
                    CreatedAt = DateTime.UtcNow,
                    FileName = fileName,
                };
                property.Images.Add(propertyImage);
                await fileStorageService.UploadFileAsync(fileName, image.Stream, image.ContentType);
            }

            await propertyRepository.AddAsync(property);
            await propertyRepository.SaveChangesAsync();

            logger.LogInformation("Объект недвижимости создан. ID: {PropertyId}", property.Id);
            return Result<int>.Success(SuccessType.Created, property.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании объекта недвижимости: {ErrorMessage}", ex.Message);
            return Result<int>.Failure(new Error(ex.Message, ErrorType.ServerError));
        }
    }

    public async Task<Result<List<PropertySearchResponse>>> SearchPropertiesAsync(PropertySearchRequest propertySearchRequest, int? userId)
    {
        logger.LogDebug(
            "Поиск объектов недвижимости. Город: {City}, Даты: {StartDate}-{EndDate}, Гостей: {Adults}+{Children}", 
            propertySearchRequest.City,
            propertySearchRequest.StartDate,
            propertySearchRequest.EndDate,
            propertySearchRequest.Adults,
            propertySearchRequest.Children
        );

        if (!context.Locations.Any(l => l.City == propertySearchRequest.City))
        {
            logger.LogWarning("Город не найден: {City}", propertySearchRequest.City);
            return Result<List<PropertySearchResponse>>.Failure(
                new Error("Location not found", ErrorType.NotFound));
        }

        var properties = await context.Properties
            .Include(p => p.Location)
            .Include(p => p.Reviews)
            .Include(p => p.Images)
            .Include(p => p.Bookings)
            .Where(p => p.IsActive && 
                        p.IsDeleted == false && 
                        p.PetsAllowed == propertySearchRequest.HasPets &&
                        p.Location.City == propertySearchRequest.City &&
                        p.MaxGuests >= propertySearchRequest.Adults + propertySearchRequest.Children &&
                        !p.Bookings.Any(b => b.Status == BookingStatus.Approved &&
                                             b.CheckInDate < propertySearchRequest.EndDate.ToUniversalTime() &&
                                             b.CheckOutDate > propertySearchRequest.StartDate.ToUniversalTime()))
            .ToListAsync();
        
        var propertySearchResponse = new List<PropertySearchResponse>();
        foreach (var property in properties)
        {
            var propertyResponse = mapper.Map<PropertySearchResponse>(property);
            propertyResponse.IsFavorite = userId is not null && context.Users
                .Include(u => u.FavoriteProperties)
                .Where(u => userId == u.Id)
                .SelectMany(u => u.FavoriteProperties)
                .Any(p => p.Id == property.Id);
            propertyResponse.TotalPrice = property.PricePerDay * (decimal)(propertySearchRequest.EndDate - propertySearchRequest.StartDate).TotalDays;
            
            var mainImageResult = await GetMainImageAsync(property.Images.First(i => i.IsMain).Id);
            if (!mainImageResult.IsSuccess)
            {
                logger.LogError("Ошибка при получении главного изображения: {Error}", mainImageResult.Error);
                return Result<List<PropertySearchResponse>>.Failure(mainImageResult.Error!);
            }
            
            propertyResponse.MainImage = mainImageResult.Value!;
            propertySearchResponse.Add(propertyResponse);
        }

        if (propertySearchResponse.Count == 0)
        {
            logger.LogInformation("Объекты не найдены для указанных критериев");
            return Result<List<PropertySearchResponse>>.Success(SuccessType.NoContent, propertySearchResponse);
        }

        logger.LogDebug("Найдено {Count} объектов", propertySearchResponse.Count);
        return Result<List<PropertySearchResponse>>.Success(SuccessType.Ok, propertySearchResponse);
    }

    public async Task<Result<PropertyDetailsResponse>> GetPropertyDetailsAsync(int propertyId)
    {
        logger.LogDebug("Запрос деталей объекта недвижимости. ID: {PropertyId}", propertyId);

        var property = await context.Properties
            .Include(p => p.Location)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .Include(p => p.Amenities)
            .FirstOrDefaultAsync(p => p.Id == propertyId);

        if (property is null)
        {
            logger.LogWarning("Объект недвижимости не найден. ID: {PropertyId}", propertyId);
            return Result<PropertyDetailsResponse>.Failure(new Error("Property not found", ErrorType.NotFound));
        }

        var propertyResponse = mapper.Map<PropertyDetailsResponse>(property);
        var imagesResult = await GetAllPropertyImagesAsync(property.Images.Select(i => i.Id).ToList());
        if (!imagesResult.IsSuccess)
        {
            logger.LogError("Ошибка при получении изображений: {Error}", imagesResult.Error);
            return Result<PropertyDetailsResponse>.Failure(imagesResult.Error!);
        }

        propertyResponse.Images = imagesResult.Value!;
        
        logger.LogDebug("Детали объекта получены. ID: {PropertyId}", propertyId);
        return Result<PropertyDetailsResponse>.Success(SuccessType.Ok, propertyResponse);
    }

    public async Task<Result<List<PropertySearchResponse>>> GetFavoritePropertiesAsync(int userId)
    {
        logger.LogDebug("Запрос избранных объектов пользователя. UserID: {UserId}", userId);

        var favoritePropertiesIds = await context.Users
            .Include(u => u.FavoriteProperties)
            .Where(u => u.Id == userId)
            .SelectMany(u => u.FavoriteProperties.Select(p => p.Id))
            .ToListAsync();

        var favoriteProperties = await context.Properties
            .Where(p => favoritePropertiesIds.Contains(p.Id))
            .Include(p => p.Location)
            .Include(p => p.Reviews)
            .Include(p => p.Images)
            .ToListAsync();
        
        var favoritePropertiesResponse = new List<PropertySearchResponse>();
        foreach (var property in favoriteProperties)
        {
            var propertyResponse = mapper.Map<PropertySearchResponse>(property);
            propertyResponse.IsFavorite = true;
            
            var mainImageResult = await GetMainImageAsync(property.Images.First(i => i.IsMain).Id);
            if (!mainImageResult.IsSuccess)
            {
                logger.LogWarning("Ошибка при получении главного изображения: {Error}", mainImageResult.Error);
                return Result<List<PropertySearchResponse>>.Failure(mainImageResult.Error!);
            }
            
            propertyResponse.MainImage = mainImageResult.Value!;
            favoritePropertiesResponse.Add(propertyResponse);
        }

        if (favoriteProperties.Count == 0)
        {
            logger.LogInformation("У пользователя нет избранных объектов");
            return Result<List<PropertySearchResponse>>.Success(SuccessType.NoContent, favoritePropertiesResponse);
        }

        logger.LogDebug("Найдено {Count} избранных объектов", favoritePropertiesResponse.Count);
        return Result<List<PropertySearchResponse>>.Success(SuccessType.Ok, favoritePropertiesResponse);
    }

    public async Task<Result<List<OwnerProperty>>> GetOwnerPropertyAsync(int userId)
    {
        var properties = await context.Properties
            .Where(p => p.OwnerId == userId)
            .ToListAsync();
        var response = mapper.Map<List<OwnerProperty>>(properties);
        return Result<List<OwnerProperty>>.Success(properties.Count == 0 
            ? SuccessType.NoContent 
            : SuccessType.Ok, response);
    }

    public async Task<Result> LikePropertyAsync(int propertyId, int userId)
    {
        var property = await context.Properties
            .FindAsync(propertyId);
        if (property is null)
        {
            logger.LogError("Property with {id} not found", propertyId);
            return Result.Failure(new Error("Property not found", ErrorType.NotFound));
        }
        var user = await context.Users.FindAsync(userId);
        if (user is null)
        {
            logger.LogError("User with {id} not found", userId);
            return Result.Failure(new Error("User not found", ErrorType.NotFound));
        }
        user.FavoriteProperties.Add(property);
        await context.SaveChangesAsync();
        logger.LogInformation("Property with {id} was liked", propertyId);
        return Result.Success(SuccessType.NoContent);
    }

    public async Task<Result> DislikePropertyAsync(int propertyId, int userId)
    {
        var property = await context.Properties
            .FindAsync(propertyId);
        if (property is null)
        {
            logger.LogError("Property with {id} not found", propertyId);
            return Result.Failure(new Error("Property not found", ErrorType.NotFound));
        }
        var user = await context.Users.FindAsync(userId);
        if (user is null)
        {
            logger.LogError("User with {id} not found", userId);
            return Result.Failure(new Error("User not found", ErrorType.NotFound));
        }
        user.FavoriteProperties.Remove(property);
        await context.SaveChangesAsync();
        logger.LogInformation("Property with {id} was disliked", propertyId);
        return Result.Success(SuccessType.NoContent);
    }

    public async Task<Result<ImageFileResponse>> GetMainImageAsync(int imageId)
    {
        logger.LogDebug("Запрос главного изображения. ImageID: {ImageId}", imageId);

        var mainImage = await context.PropertyImages
            .Where(i => i.Id == imageId)
            .Select(i => new { i.IsMain, i.FileName })
            .FirstOrDefaultAsync();

        if (mainImage is null)
        {
            logger.LogWarning("Изображение не найдено. ImageID: {ImageId}", imageId);
            return Result<ImageFileResponse>.Failure(new Error("Image not found", ErrorType.NotFound));
        }

        if (!mainImage.IsMain)
        {
            logger.LogWarning("Изображение не является главным. ImageID: {ImageId}", imageId);
            return Result<ImageFileResponse>.Failure(new Error("Image is not main", ErrorType.BadRequest));
        }

        var contentResult = await fileStorageService.DownloadFileAsBase64Async(mainImage.FileName);
        if (!contentResult.IsSuccess)
        {
            logger.LogError("Ошибка загрузки изображения. FileName: {FileName}, Error: {Error}", 
                mainImage.FileName, contentResult.Error);
            return Result<ImageFileResponse>.Failure(contentResult.Error!);
        }

        var imageFileResponse = new ImageFileResponse
        {
            IsMain = true,
            ContentBase64 = contentResult.Value!,
            MimeType = fileWorker.DefineMimeType(mainImage.FileName)
        };
        
        logger.LogDebug("Главное изображение получено. ImageID: {ImageId}", imageId);
        return Result<ImageFileResponse>.Success(SuccessType.Ok, imageFileResponse);
    }

    public async Task<Result<List<ImageFileResponse>>> GetAllPropertyImagesAsync(List<int> imagesId)
    {
        logger.LogDebug("Запрос изображений объекта. ImageIDs: {ImageIds}", string.Join(", ", imagesId));

        var images = await context.PropertyImages
            .Where(i => imagesId.Contains(i.Id))
            .Select(i => new { i.IsMain, i.FileName })
            .ToListAsync();
        
        var imagesResponse = new List<ImageFileResponse>();
        foreach (var image in images)
        {
            var contentResult = await fileStorageService.DownloadFileAsBase64Async(image.FileName);
            if (!contentResult.IsSuccess)
            {
                logger.LogError("Ошибка загрузки изображения. FileName: {FileName}, Error: {Error}", 
                    image.FileName, contentResult.Error);
                return Result<List<ImageFileResponse>>.Failure(contentResult.Error!);
            }

            imagesResponse.Add(new ImageFileResponse
            {
                IsMain = image.IsMain,
                ContentBase64 = contentResult.Value!,
                MimeType = fileWorker.DefineMimeType(image.FileName)
            });
        }

        if (images.Count == 0)
        {
            logger.LogInformation("Изображения не найдены");
            return Result<List<ImageFileResponse>>.Success(SuccessType.NoContent, imagesResponse);
        }

        logger.LogDebug("Получено {Count} изображений", imagesResponse.Count);
        return Result<List<ImageFileResponse>>.Success(SuccessType.Ok, imagesResponse);
    }
}