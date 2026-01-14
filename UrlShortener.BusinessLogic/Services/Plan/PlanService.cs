using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Mappers;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Repositories.Plan;

namespace UrlShortener.BusinessLogic.Services.Plan;

public class PlanService : IPlanService
{
    private readonly IPlanRepository _plansRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlanService> _logger;

    private const string CacheKey_AllPlans = "plans:all";
    private static string CacheKey_PlanById(Guid id) => $"plans:id:{id}";

    public PlanService(
        IPlanRepository plansRepository,
        IMemoryCache cache,
        ILogger<PlanService> logger)
    {
        _plansRepository = plansRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ServiceResponse<List<PlanDto>>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey_AllPlans, out List<PlanDto>? cachedDtos) && cachedDtos is not null)
        {
            _logger.LogDebug("Plans loaded from cache.");
            return ServiceResponse<List<PlanDto>>.Ok(cachedDtos);
        }

        _logger.LogInformation("Loading all plans from database.");

        var planEntities = await _plansRepository.GetAllAsync(ct);
        var planDtos = planEntities.Select(plan => plan.ToDto()).ToList();

        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
            .SetSize(1);

        _cache.Set(CacheKey_AllPlans, planDtos, options);

        _logger.LogInformation("Cached {Count} plans.", planDtos.Count);

        return ServiceResponse<List<PlanDto>>.Ok(planDtos);
    }

    public async Task<ServiceResponse<PlanDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("GetById called with empty id.");
            return ServiceResponse<PlanDto>.Fail("Invalid id.");
        }

        var key = CacheKey_PlanById(id);
        if (_cache.TryGetValue(key, out PlanDto? cachedDto) && cachedDto is not null)
        {
            _logger.LogDebug("Plan {PlanId} loaded from cache.", id);
            return ServiceResponse<PlanDto>.Ok(cachedDto);
        }

        _logger.LogInformation("Loading plan {PlanId} from database.", id);

        var planEntity = await _plansRepository.GetByIdAsync(id, ct);
        if (planEntity is null)
        {
            _logger.LogWarning("Plan {PlanId} not found.", id);
            return ServiceResponse<PlanDto>.Fail("Plan not found.");
        }

        var dto = planEntity.ToDto();

        var options = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(10));

        _cache.Set(key, dto, options);

        _logger.LogInformation("Plan {PlanId} cached.", id);

        return ServiceResponse<PlanDto>.Ok(dto);
    }

    public async Task<ServiceResponse<PlanDto>> CreateAsync(PlanDto planDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new plan {PlanName}.", planDto.Name);

        var planEntity = planDto.ToEntity();

        try
        {
            await _plansRepository.AddAsync(planEntity, ct);
            await _plansRepository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan {PlanName}.", planDto.Name);
            return ServiceResponse<PlanDto>.Fail("Error creating plan.");
        }

        var createdDto = planEntity.ToDto();

        _cache.Remove(CacheKey_AllPlans);
        _cache.Set(CacheKey_PlanById(createdDto.Id), createdDto, TimeSpan.FromMinutes(10));

        _logger.LogInformation("Plan {PlanId} created and cache updated.", createdDto.Id);

        return ServiceResponse<PlanDto>.Ok(createdDto);
    }

    public async Task<ServiceResponse<PlanDto>> UpdateAsync(PlanDto planDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating plan {PlanId}.", planDto.Id);

        var entity = await _plansRepository.GetByIdAsync(planDto.Id, ct);
        if (entity is null)
        {
            _logger.LogWarning("Update failed. Plan {PlanId} not found.", planDto.Id);
            return ServiceResponse<PlanDto>.Fail("Plan not found.");
        }

        try
        {
            _plansRepository.Update(planDto.ToEntity(entity));
            await _plansRepository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan {PlanId}.", planDto.Id);
            return ServiceResponse<PlanDto>.Fail("Error updating plan.");
        }

        var updatedDto = entity.ToDto();

        _cache.Remove(CacheKey_AllPlans);
        _cache.Set(CacheKey_PlanById(updatedDto.Id), updatedDto, TimeSpan.FromMinutes(10));

        _logger.LogInformation("Plan {PlanId} updated and cache refreshed.", updatedDto.Id);

        return ServiceResponse<PlanDto>.Ok(updatedDto);
    }

    public async Task<ServiceResponse> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("Delete called with empty id.");
            return ServiceResponse.Fail("Invalid id.");
        }

        _logger.LogInformation("Deleting plan {PlanId}.", id);

        var entity = await _plansRepository.GetByIdAsync(id, ct);
        if (entity is null)
        {
            _logger.LogWarning("Delete failed. Plan {PlanId} not found.", id);
            return ServiceResponse.Fail("Plan not found.");
        }

        if (entity.Subscriptions is { Count: > 0 })
        {
            _logger.LogWarning(
                "Delete blocked. Plan {PlanId} has active subscriptions.",
                id);

            return ServiceResponse.Fail(
                "Plan cannot be deleted because it has subscriptions.");
        }

        try
        {
            _plansRepository.Remove(entity);
            await _plansRepository.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting plan {PlanId}.", id);
            return ServiceResponse.Fail("Error deleting plan.");
        }

        _cache.Remove(CacheKey_AllPlans);
        _cache.Remove(CacheKey_PlanById(id));

        _logger.LogInformation("Plan {PlanId} deleted and cache invalidated.", id);

        return ServiceResponse.Ok("Plan deleted.");
    }
}
