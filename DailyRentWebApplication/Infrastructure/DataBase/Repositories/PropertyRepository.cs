using Domain.Abstractions.Repositories;
using Domain.Entities;
using Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataBase.Repositories;

public class PropertyRepository(AppDbContext context) : Repository<Property>(context), IPropertyRepository
{
    private readonly AppDbContext _context = context;

    public async Task<bool> AnyLocationByCityAsync(string city)
    {
        return await _context.Locations.AnyAsync(l => l.City == city);
    }

    public async Task<List<Property>> SearchPropertiesAsync(
        string city,
        bool hasPets,
        int guestsCount,
        DateTime startDate,
        DateTime endDate)
    {
        return await _context.Properties
            .Include(p => p.Location)
            .Include(p => p.Reviews)
            .Include(p => p.Images)
            .Include(p => p.Bookings)
            .Where(p => p.IsActive &&
                        p.IsDeleted == false &&
                        p.PetsAllowed == hasPets &&
                        p.Location.City == city &&
                        p.MaxGuests >= guestsCount &&
                        !p.Bookings.Any(b => b.Status == BookingStatus.Approved &&
                                             b.CheckInDate < endDate.ToUniversalTime() &&
                                             b.CheckOutDate > startDate.ToUniversalTime()))
            .ToListAsync();
    }

    public async Task<List<Property>> GetPropertiesWithDetailsAsync(int propertyId)
    {
        return await _context.Properties
            .Include(p => p.Location)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .Include(p => p.Amenities)
            .Where(p => p.Id == propertyId)
            .ToListAsync();
    }

    public async Task<List<int>> GetFavoritePropertiesIdsAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.FavoriteProperties)
            .Where(u => u.Id == userId)
            .SelectMany(u => u.FavoriteProperties.Select(p => p.Id))
            .ToListAsync();
    }

    public async Task<List<Property>> GetFavoritePropertiesAsync(List<int> favoritePropertiesIds)
    {
        return await _context.Properties
            .Where(p => favoritePropertiesIds.Contains(p.Id))
            .Include(p => p.Location)
            .Include(p => p.Reviews)
            .Include(p => p.Images)
            .ToListAsync();
    }

    public async Task<List<Property>> GetOwnerPropertiesAsync(int userId)
    {
        return await _context.Properties
            .Where(p => p.OwnerId == userId)
            .ToListAsync();
    }

    public async Task<Property?> GetPropertyWithUserAsync(int propertyId)
    {
        return await _context.Properties
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == propertyId);
    }

    public async Task<PropertyImage?> GetImageInfoAsync(int imageId)
    {
        return await _context.PropertyImages
            .FirstOrDefaultAsync(i => i.Id == imageId);
    }

    public async Task<List<PropertyImage>> GetImagesInfoAsync(List<int> imagesId)
    {
        return await _context.PropertyImages
            .Where(i => imagesId.Contains(i.Id))
            .ToListAsync();
    }
}