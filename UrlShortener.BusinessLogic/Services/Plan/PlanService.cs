using Microsoft.Extensions.Caching.Memory;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Mappers;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Repositories.Plan;

namespace UrlShortener.BusinessLogic.Services.Plan;

public class PlanService : IPlanService
{
    private readonly IPlanRepository _plansRepository;
    private readonly IMemoryCache _cache;

    private const string CacheKey_AllPlans = "plans:all";
    private static string CacheKey_PlanById(Guid id) => $"plans:id:{id}";

    public PlanService(IPlanRepository plansRepository, IMemoryCache cache)
    {
        _plansRepository = plansRepository;
        _cache = cache;
    }

    public async Task<ServiceResponse<List<PlanDto>>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey_AllPlans, out List<PlanDto>? cachedDtos) && cachedDtos is not null)
            return ServiceResponse<List<PlanDto>>.Ok(cachedDtos);

        var planEntities = await _plansRepository.GetAllAsync(ct);
        var planDtos = planEntities.Select(plan => plan.ToDto()).ToList();

        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
            .SetSize(1);

        _cache.Set(CacheKey_AllPlans, planDtos, options);

        return ServiceResponse<List<PlanDto>>.Ok(planDtos);
    }

    public async Task<ServiceResponse<PlanDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return ServiceResponse<PlanDto>.Fail("Invalid id.");

        var key = CacheKey_PlanById(id);
        if (_cache.TryGetValue(key, out PlanDto? cachedDto) && cachedDto is not null)
            return ServiceResponse<PlanDto>.Ok(cachedDto);

        var planEntity = await _plansRepository.GetByIdAsync(id, ct);
        if (planEntity is null)
            return ServiceResponse<PlanDto>.Fail("Plan not found.");

        var dto = planEntity.ToDto();

        var options = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(10));

        _cache.Set(key, dto, options);

        return ServiceResponse<PlanDto>.Ok(dto);
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

        var createdDto = planEntity.ToDto();

        _cache.Remove(CacheKey_AllPlans);
        _cache.Set(CacheKey_PlanById(createdDto.Id), createdDto, TimeSpan.FromMinutes(10));

        return ServiceResponse<PlanDto>.Ok(createdDto);
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

        var updatedDto = entity.ToDto();

        _cache.Remove(CacheKey_AllPlans);
        _cache.Set(CacheKey_PlanById(updatedDto.Id), updatedDto, TimeSpan.FromMinutes(10));

        return ServiceResponse<PlanDto>.Ok(updatedDto);
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

        _cache.Remove(CacheKey_AllPlans);
        _cache.Remove(CacheKey_PlanById(id));

        return ServiceResponse.Ok("Plan deleted.");
    }
}