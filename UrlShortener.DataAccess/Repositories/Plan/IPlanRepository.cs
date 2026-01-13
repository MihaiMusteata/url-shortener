using UrlShortener.DataAccess.Entities;

namespace UrlShortener.DataAccess.Repositories.Plan;


public interface IPlanRepository
{
    Task<PlanDbTable?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PlanDbTable>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(PlanDbTable plan, CancellationToken ct = default);
    void Update(PlanDbTable plan);
    void Remove(PlanDbTable plan);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
