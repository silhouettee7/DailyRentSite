using Domain.Abstractions.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataBase.Repositories;

public class RefreshSessionRepository(AppDbContext context)
    : Repository<RefreshSession>(context), IRefreshSessionRepository
{
    public async Task<RefreshSession?> GetSessionByIdWithUser(Guid sessionId)
    {
        return await context.RefreshSessions
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == sessionId);
    }

    public async Task DeleteAllUserSessions(int userId)
    {
        await context.RefreshSessions
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync();
    }
}