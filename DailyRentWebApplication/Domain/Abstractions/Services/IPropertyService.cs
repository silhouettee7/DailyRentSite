using Domain.Entities;
using Domain.Models.Dtos;
using Domain.Models.Dtos.Image;
using Domain.Models.Dtos.Property;
using Domain.Models.Result;

namespace Domain.Abstractions.Services;

public interface IPropertyService
{
    Task<Result<int>> CreatePropertyAsync(PropertyCreateRequest propertyCreateRequest, int userId);
    Task<Result<List<PropertySearchResponse>>> SearchPropertiesAsync(PropertySearchRequest propertySearchRequest, int? userId);
    Task<Result<PropertyDetailsResponse>> GetPropertyDetailsAsync(int propertyId);
    Task<Result<List<PropertySearchResponse>>> GetFavoritePropertiesAsync(int userId);
    Task<Result<List<OwnerProperty>>> GetOwnerPropertyAsync(int userId);
    Task<Result> LikePropertyAsync(int propertyId, int userId);
    Task<Result> DislikePropertyAsync(int propertyId, int userId);
    Task<Result<ImageFileResponse>> GetMainImageAsync(int imageId);
    Task<Result<List<ImageFileResponse>>> GetAllPropertyImagesAsync(List<int> imagesId);
}