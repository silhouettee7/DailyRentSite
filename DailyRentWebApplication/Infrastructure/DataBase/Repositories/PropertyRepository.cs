using Domain.Abstractions.Repositories;
using Domain.Entities;

namespace Infrastructure.DataBase.Repositories;

public class PropertyRepository(AppDbContext context) : Repository<Property>(context), IPropertyRepository;