using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess.Repositories.User;

public interface IUserRepository
{
    Task<UserDbTable?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDbTable?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<UserDbTable>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(UserDbTable user, CancellationToken ct = default);
    void Update(UserDbTable user);
    void Remove(UserDbTable user);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}