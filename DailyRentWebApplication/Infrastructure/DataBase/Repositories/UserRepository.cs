using Domain.Abstractions.Repositories;
using Domain.Entities;

namespace Infrastructure.DataBase.Repositories;

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository;