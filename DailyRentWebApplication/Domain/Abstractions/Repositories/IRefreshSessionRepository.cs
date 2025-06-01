using Domain.Entities;

namespace Domain.Abstractions.Repositories;

public interface IRefreshSessionRepository: IRepository<RefreshSession>
{
    Task<RefreshSession?> GetSessionByIdWithUser(Guid sessionId);
    Task DeleteAllUserSessions(int userId);
}