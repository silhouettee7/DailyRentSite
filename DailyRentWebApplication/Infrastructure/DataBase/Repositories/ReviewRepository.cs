using Domain.Abstractions.Repositories;
using Domain.Entities;

namespace Infrastructure.DataBase.Repositories;

public class ReviewRepository(AppDbContext context) : Repository<Review>(context), IReviewRepository;