using Api.Utils;
using AutoMapper;
using Domain.Abstractions.Repositories;
using Domain.Abstractions.Services;
using Domain.Entities;
using Domain.Models.Dtos;
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
    AppDbContext context): IPropertyService
{
    public async Task<Result<int>> CreatePropertyAsync(PropertyCreateRequest propertyCreateRequest, int userId)
    {
        var property = mapper.Map<Property>(propertyCreateRequest);
        property.OwnerId = userId;
        try
        {
            var mainImage = propertyCreateRequest.MainImage;
            var mainImageFileName =  $"main_{Guid.NewGuid()}_" + mainImage.FileName;
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
            return Result<int>.Success(SuccessType.Created,property.Id);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(new Error(ex.Message, ErrorType.ServerError));
        }
    }

    public async Task<Result<List<PropertySearchResponse>>> SearchPropertiesAsync(PropertySearchRequest propertySearchRequest, int? userId)
    {
        if (!context.Locations.Any(l => l.City == propertySearchRequest.City))
        {
            return Result<List<PropertySearchResponse>>.Failure(
                new Error("Location not found", ErrorType.NotFound));
        }
        var startDate = propertySearchRequest.StartDate;
        var endDate = propertySearchRequest.EndDate;
        var properties = await context.Properties
            .Include(p => p.Location)
            .Include(p => p.Reviews)
            .Include(p => p.Images)
            .Where(p => p.IsActive && 
                        p.IsDeleted == false && 
                        p.PetsAllowed == propertySearchRequest.HasPets &&
                        p.Location.City == propertySearchRequest.City &&
                        p.MaxGuests >= propertySearchRequest.Adults + propertySearchRequest.Children &&
                        !p.Bookings.Any(b => b.Status == BookingStatus.Approved &&
                                             b.CheckInDate < endDate&&
                                             b.CheckOutDate > startDate))
            .ToListAsync();
        var propertySearchResponse = mapper.Map<List<PropertySearchResponse>>(properties);
        propertySearchResponse.ForEach(property =>
        {
            property.IsFavorite = userId is not null && context.Users
                .Include(u => u.FavoriteProperties)
                .Where(u => userId == u.Id)
                .SelectMany(u => u.FavoriteProperties)
                .Any(p => p.Id == property.Id);
            property.TotalPrice = property.PricePerDay * (decimal)(endDate - startDate).TotalDays;
        });
        if (propertySearchResponse.Count == 0)
        {
            return Result<List<PropertySearchResponse>>.Success(SuccessType.NoContent, propertySearchResponse);
        }
        return Result<List<PropertySearchResponse>>.Success(SuccessType.Ok,propertySearchResponse);
    }

    public async Task<Result<PropertyDetailsResponse>> GetPropertyDetailsAsync(int propertyId)
    {
        var property = await context.Properties
            .Include(p => p.Location)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .Include(p => p.Amenities)
            .FirstOrDefaultAsync(p => p.Id == propertyId);
        if (property is null)
        {
            return Result<PropertyDetailsResponse>.Failure(new Error("Property not found", ErrorType.NotFound));
        }
        var propertyResponse = mapper.Map<PropertyDetailsResponse>(property);
        return Result<PropertyDetailsResponse>.Success(SuccessType.Ok,propertyResponse);
    }

    public async Task<Result<List<PropertySearchResponse>>> GetFavoritePropertiesAsync(int userId)
    {
        var favoriteProperties = await context.Users
            .Include(u => u.FavoriteProperties)
            .Where(u => u.Id == userId)
            .SelectMany(u => u.FavoriteProperties)
            .ToListAsync();
        
        var favoritePropertiesResponse = mapper.Map<List<PropertySearchResponse>>(favoriteProperties);
        if (favoriteProperties.Count == 0)
        {
            return Result<List<PropertySearchResponse>>.Success(SuccessType.NoContent, favoritePropertiesResponse);
        }
        return Result<List<PropertySearchResponse>>.Success(SuccessType.Ok, favoritePropertiesResponse);
    }

    public async Task<Result<ImageFileResponse>> GetMainImageAsync(int imageId)
    {
        var mainImage = await context.PropertyImages
            .Where(i => i.Id == imageId)
            .Select(i => new
            {
                i.IsMain,
                i.FileName,
            })
            .FirstOrDefaultAsync();
        if (mainImage is null)
        {
            return Result<ImageFileResponse>.Failure(new Error("Image not found", ErrorType.NotFound));
        }

        if (!mainImage.IsMain)
        {
            return Result<ImageFileResponse>.Failure(new Error("Image is not main", ErrorType.BadRequest));
        }
        var contentResult = await fileStorageService.DownloadFileAsBase64Async(mainImage.FileName);
        if (!contentResult.IsSuccess)
        {
            return Result<ImageFileResponse>.Failure(contentResult.Error!);
        }

        var imageFileResponse = new ImageFileResponse
        {
            IsMain = true,
            ContentBase64 = contentResult.Value!,
            MimeType = fileWorker.DefineMimeType(mainImage.FileName)
        };
        
        return Result<ImageFileResponse>.Success(SuccessType.Ok,imageFileResponse);
    }

    public async Task<Result<List<ImageFileResponse>>> GetAllPropertyImagesAsync(List<int> imagesId)
    {
        var images = await context.PropertyImages
            .Where(i => imagesId.Contains(i.Id))
            .Select(i => new
            {
                i.IsMain,
                i.FileName,
            })
            .ToListAsync();
        
        var imagesResponse = new List<ImageFileResponse>();
        foreach (var image in images)
        {
            var contentResult = await fileStorageService.DownloadFileAsBase64Async(image.FileName);
            if (!contentResult.IsSuccess)
            {
                return Result<List<ImageFileResponse>>.Failure(contentResult.Error!);
            }

            var imageResponse = new ImageFileResponse
            {
                IsMain = image.IsMain,
                ContentBase64 = contentResult.Value!,
                MimeType = fileWorker.DefineMimeType(image.FileName)
            };
            imagesResponse.Add(imageResponse);
        }
        if (images.Count == 0)
        {
            return Result<List<ImageFileResponse>>.Success(SuccessType.NoContent, imagesResponse);
        }
        return Result<List<ImageFileResponse>>.Success(SuccessType.Ok, imagesResponse);
    }
}