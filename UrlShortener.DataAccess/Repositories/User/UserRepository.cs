using Microsoft.EntityFrameworkCore;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess.Repositories.User;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<UserDbTable?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<UserDbTable?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

    public Task<List<UserDbTable>> GetAllAsync(CancellationToken ct = default)
        => _db.Users.AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(UserDbTable user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    public void Update(UserDbTable user)
        => _db.Users.Update(user);

    public void Remove(UserDbTable user)
        => _db.Users.Remove(user);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}