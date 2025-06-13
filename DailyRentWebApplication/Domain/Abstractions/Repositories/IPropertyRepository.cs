using Domain.Entities;

namespace Domain.Abstractions.Repositories;


public interface IPropertyRepository : IRepository<Property>
{
    Task<bool> AnyLocationByCityAsync(string city);
    Task<List<Property>> SearchPropertiesAsync(
        string city,
        bool hasPets,
        int guestsCount,
        DateTime startDate,
        DateTime endDate);
    Task<List<Property>> GetPropertiesWithDetailsAsync(int propertyId);
    Task<List<int>> GetFavoritePropertiesIdsAsync(int userId);
    Task<List<Property>> GetFavoritePropertiesAsync(List<int> favoritePropertiesIds);
    Task<List<Property>> GetOwnerPropertiesAsync(int userId);
    Task<Property?> GetPropertyWithUserAsync(int propertyId);
    Task<PropertyImage?> GetImageInfoAsync(int imageId);
    Task<List<PropertyImage>> GetImagesInfoAsync(List<int> imagesId);
}