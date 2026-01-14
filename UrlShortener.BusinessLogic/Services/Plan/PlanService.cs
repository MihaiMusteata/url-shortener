using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Mappers;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Repositories.Plan;

namespace UrlShortener.BusinessLogic.Services.Plan;

public class PlanService : IPlanService
{
    private readonly IPlanRepository _plansRepository;

    public PlanService(IPlanRepository plansRepository)
    {
        _plansRepository = plansRepository;
    }

    public async Task<ServiceResponse<List<PlanDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var planEntities = await _plansRepository.GetAllAsync(ct);
        var planDtos = planEntities.Select(plan => plan.ToDto()).ToList();
        return ServiceResponse<List<PlanDto>>.Ok(planDtos);
    }

    public async Task<ServiceResponse<PlanDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return ServiceResponse<PlanDto>.Fail("Invalid id.");

        var planEntity = await _plansRepository.GetByIdAsync(id, ct);
        if (planEntity is null)
            return ServiceResponse<PlanDto>.Fail("Plan not found.");

        return ServiceResponse<PlanDto>.Ok(planEntity.ToDto());
    }

    public async Task<ServiceResponse<PlanDto>> CreateAsync(PlanDto planDto, CancellationToken ct = default)
    {
        var planEntity = planDto.ToEntity();

        try
        {
            await _plansRepository.AddAsync(planEntity, ct);
            await _plansRepository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            return ServiceResponse<PlanDto>.Fail($"Error creating plan: {ex.Message}");
        }

        return ServiceResponse<PlanDto>.Ok(planEntity.ToDto());
    }

    public async Task<ServiceResponse<PlanDto>> UpdateAsync(PlanDto planDto, CancellationToken ct = default)
    {
        var entity = await _plansRepository.GetByIdAsync(planDto.Id, ct);
        if (entity is null)
            return ServiceResponse<PlanDto>.Fail("Plan not found.");

        try
        {
            _plansRepository.Update(planDto.ToEntity(entity));
            await _plansRepository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            return ServiceResponse<PlanDto>.Fail($"Error updating plan: {ex.Message}");
        }

        return ServiceResponse<PlanDto>.Ok(entity.ToDto());
    }

    public async Task<ServiceResponse> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return ServiceResponse.Fail("Invalid id.");

        var entity = await _plansRepository.GetByIdAsync(id, ct);
        if (entity is null)
            return ServiceResponse.Fail("Plan not found.");

        if (entity.Subscriptions is { Count: > 0 })
            return ServiceResponse.Fail("Plan cannot be deleted because it has subscriptions.");

        try
        {
            _plansRepository.Remove(entity);
            await _plansRepository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            return ServiceResponse.Fail($"Error deleting plan: {ex.Message}");
        }

        return ServiceResponse.Ok("Plan deleted.");
    }
}