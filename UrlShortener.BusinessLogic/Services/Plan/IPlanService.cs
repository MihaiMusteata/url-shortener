using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Wrappers;

namespace UrlShortener.BusinessLogic.Services.Plan;

public interface IPlanService
{
    Task<ServiceResponse<List<PlanDto>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResponse<PlanDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResponse<PlanDto>> CreateAsync(PlanDto planDto, CancellationToken ct = default);
    Task<ServiceResponse<PlanDto>> UpdateAsync(PlanDto planDto, CancellationToken ct = default);
    Task<ServiceResponse> DeleteAsync(Guid id, CancellationToken ct = default);
}
