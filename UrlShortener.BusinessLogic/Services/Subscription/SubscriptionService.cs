using Microsoft.Extensions.Caching.Memory;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Mappers;
using UrlShortener.BusinessLogic.Services.Profile;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Entities;
using UrlShortener.DataAccess.Repositories.Plan;
using UrlShortener.DataAccess.Repositories.Subscription;
using UrlShortener.DataAccess.Repositories.User;

namespace UrlShortener.BusinessLogic.Services.Subscription;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subs;
    private readonly IUserRepository _users;
    private readonly IPlanRepository _plans;
    private readonly IProfileService _profileService;
    private readonly IMemoryCache _cache;

    private const string CacheKey_All = "subs:all";
    private static string CacheKey_ById(Guid id) => $"subs:id:{id}";
    private static string CacheKey_ByUser(Guid userId) => $"subs:user:{userId}";
    private static string CacheKey_Active(Guid userId) => $"subs:active:{userId}";
    private static string CacheKey_CurrentPlan(Guid userId) => $"subs:currentplan:{userId}";

    public SubscriptionService(
        ISubscriptionRepository subs,
        IUserRepository users,
        IPlanRepository plans,
        IProfileService profileService,
        IMemoryCache cache)
    {
        _subs = subs;
        _users = users;
        _plans = plans;
        _profileService = profileService;
        _cache = cache;
    }

    public async Task<ServiceResponse<List<SubscriptionDetailsDto>>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey_All, out List<SubscriptionDetailsDto>? cached) && cached is not null)
            return ServiceResponse<List<SubscriptionDetailsDto>>.Ok(cached);

        var items = await _subs.GetAllAsync(ct);
        var dtos = items.Select(x => x.ToDetailsDto()).ToList();

        _cache.Set(
            CacheKey_All,
            dtos,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(2)));

        return ServiceResponse<List<SubscriptionDetailsDto>>.Ok(dtos);
    }

    public async Task<ServiceResponse<SubscriptionDetailsDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return ServiceResponse<SubscriptionDetailsDto>.Fail("Invalid id.");

        var key = CacheKey_ById(id);
        if (_cache.TryGetValue(key, out SubscriptionDetailsDto? cached) && cached is not null)
            return ServiceResponse<SubscriptionDetailsDto>.Ok(cached);

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
            return ServiceResponse<SubscriptionDetailsDto>.Fail("Subscription not found.");

        var dto = entity.ToDetailsDto();

        _cache.Set(
            key,
            dto,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(2)));

        return ServiceResponse<SubscriptionDetailsDto>.Ok(dto);
    }

    public async Task<ServiceResponse<List<SubscriptionDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<List<SubscriptionDto>>.Fail("Invalid user id.");

        var key = CacheKey_ByUser(userId);
        if (_cache.TryGetValue(key, out List<SubscriptionDto>? cached) && cached is not null)
            return ServiceResponse<List<SubscriptionDto>>.Ok(cached);

        var items = await _subs.GetByUserIdAsync(userId, ct);
        var dtos = items.Select(x => x.ToDto()).ToList();

        _cache.Set(
            key,
            dtos,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));

        return ServiceResponse<List<SubscriptionDto>>.Ok(dtos);
    }

    public async Task<ServiceResponse<SubscriptionDto>> GetActiveForUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<SubscriptionDto>.Fail("Invalid user id.");

        var key = CacheKey_Active(userId);
        if (_cache.TryGetValue(key, out SubscriptionDto? cached) && cached is not null)
            return ServiceResponse<SubscriptionDto>.Ok(cached);

        var entity = await _subs.GetActiveForUserAsync(userId, ct);
        if (entity is null)
            return ServiceResponse<SubscriptionDto>.Fail("No active subscription.");

        var dto = entity.ToDto();

        _cache.Set(
            key,
            dto,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(45)));

        return ServiceResponse<SubscriptionDto>.Ok(dto);
    }

    public async Task<ServiceResponse<SubscriptionDto>> CreateAsync(SubscriptionDto dto, CancellationToken ct = default)
    {
        var v = await ValidateCreateOrUpdate(dto, ct);
        if (!v.Success)
            return ServiceResponse<SubscriptionDto>.Fail(v.Message ?? "Validation failed.");

        var entity = dto.ToEntity();
        entity.Id = Guid.NewGuid();

        await _subs.AddAsync(entity, ct);
        await _subs.SaveChangesAsync(ct);

        var created = await _subs.GetByIdAsync(entity.Id, ct);

        InvalidateForUser(dto.UserId);
        _cache.Remove(CacheKey_All);

        return ServiceResponse<SubscriptionDto>.Ok(created!.ToDto(), "Subscription created.");
    }

    public async Task<ServiceResponse<SubscriptionDto>> UpdateAsync(SubscriptionDto dto, CancellationToken ct = default)
    {
        if (dto.Id == Guid.Empty)
            return ServiceResponse<SubscriptionDto>.Fail("Invalid id.");

        var v = await ValidateCreateOrUpdate(dto, ct);
        if (!v.Success)
            return ServiceResponse<SubscriptionDto>.Fail(v.Message ?? "Validation failed.");

        var entity = await _subs.GetByIdAsync(dto.Id, ct);
        if (entity is null)
            return ServiceResponse<SubscriptionDto>.Fail("Subscription not found.");

        var oldUserId = entity.UserId;

        entity.UserId = dto.UserId;
        entity.PlanId = dto.PlanId;
        entity.Active = dto.Active;

        _subs.Update(entity);
        await _subs.SaveChangesAsync(ct);

        var updated = await _subs.GetByIdAsync(dto.Id, ct);

        _cache.Remove(CacheKey_All);
        _cache.Remove(CacheKey_ById(dto.Id));
        InvalidateForUser(oldUserId);
        InvalidateForUser(dto.UserId);

        return ServiceResponse<SubscriptionDto>.Ok(updated!.ToDto(), "Subscription updated.");
    }

    public async Task<ServiceResponse> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return ServiceResponse.Fail("Invalid id.");

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
            return ServiceResponse.Fail("Subscription not found.");

        var userId = entity.UserId;

        _subs.Remove(entity);
        await _subs.SaveChangesAsync(ct);

        _cache.Remove(CacheKey_All);
        _cache.Remove(CacheKey_ById(id));
        InvalidateForUser(userId);

        return ServiceResponse.Ok("Subscription deleted.");
    }

    public async Task<ServiceResponse> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return ServiceResponse.Fail("Invalid id.");

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
            return ServiceResponse.Fail("Subscription not found.");

        if (entity.Active)
            return ServiceResponse.Ok("Subscription already active.");

        entity.Active = true;
        _subs.Update(entity);
        await _subs.SaveChangesAsync(ct);

        _cache.Remove(CacheKey_All);
        _cache.Remove(CacheKey_ById(id));
        InvalidateForUser(entity.UserId);

        return ServiceResponse.Ok("Subscription activated.");
    }

    public async Task<ServiceResponse> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return ServiceResponse.Fail("Invalid id.");

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
            return ServiceResponse.Fail("Subscription not found.");

        if (!entity.Active)
            return ServiceResponse.Ok("Subscription already inactive.");

        entity.Active = false;
        _subs.Update(entity);
        await _subs.SaveChangesAsync(ct);

        _cache.Remove(CacheKey_All);
        _cache.Remove(CacheKey_ById(id));
        InvalidateForUser(entity.UserId);

        return ServiceResponse.Ok("Subscription deactivated.");
    }

    private async Task<ServiceResponse> ValidateCreateOrUpdate(SubscriptionDto dto, CancellationToken ct)
    {
        if (dto.UserId == Guid.Empty)
            return ServiceResponse.Fail("UserId is required.");

        if (dto.PlanId == Guid.Empty)
            return ServiceResponse.Fail("PlanId is required.");

        var user = await _users.GetByIdAsync(dto.UserId, ct);
        if (user is null)
            return ServiceResponse.Fail("User not found.");

        var plan = await _plans.GetByIdAsync(dto.PlanId, ct);
        if (plan is null)
            return ServiceResponse.Fail("Plan not found.");

        return ServiceResponse.Ok();
    }

    public async Task<ServiceResponse<SubscriptionActionResultDto>> SubscribeAsync(
        Guid userId,
        Guid planId,
        CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResponse<SubscriptionActionResultDto>.Fail("User not found.");

        var plan = await _plans.GetByIdAsync(planId, ct);
        if (plan is null)
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Plan not found.");

        var active = await _subs.GetActiveByUserIdAsync(userId, ct);
        if (active is not null)
            return ServiceResponse<SubscriptionActionResultDto>.Fail(
                "User already has an active subscription. Use upgrade.");

        var sub = new SubscriptionDbTable
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            Active = true
        };

        await _subs.AddAsync(sub, ct);
        await _subs.SaveChangesAsync(ct);

        _cache.Remove(CacheKey_All);
        InvalidateForUser(userId);

        return ServiceResponse<SubscriptionActionResultDto>.Ok(new SubscriptionActionResultDto
        {
            SubscriptionId = sub.Id,
            UserId = userId,
            PlanId = plan.Id,
            PlanName = plan.Name,
            Active = sub.Active,
            Action = "subscribed"
        }, "Subscribed.");
    }

    public async Task<ServiceResponse<SubscriptionActionResultDto>> UpgradeAsync(
        Guid userId,
        Guid newPlanId,
        CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResponse<SubscriptionActionResultDto>.Fail("User not found.");

        var newPlan = await _plans.GetByIdAsync(newPlanId, ct);
        if (newPlan is null)
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Plan not found.");

        var current = await _subs.GetActiveByUserIdAsync(userId, ct);
        if (current is null)
            return ServiceResponse<SubscriptionActionResultDto>.Fail("No active subscription found.");

        if (current.PlanId == newPlanId)
            return ServiceResponse<SubscriptionActionResultDto>.Fail("User is already on this plan.");

        if (current.Plan is null)
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Current plan data missing.");

        if (!IsUpgrade(current.Plan, newPlan))
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Only upgrades are allowed.");

        current.Active = false;
        _subs.Update(current);

        var upgraded = new SubscriptionDbTable
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = newPlanId,
            Active = true
        };

        await _subs.AddAsync(upgraded, ct);
        await _subs.SaveChangesAsync(ct);

        _cache.Remove(CacheKey_All);
        InvalidateForUser(userId);

        return ServiceResponse<SubscriptionActionResultDto>.Ok(new SubscriptionActionResultDto
        {
            SubscriptionId = upgraded.Id,
            UserId = userId,
            PlanId = newPlan.Id,
            PlanName = newPlan.Name,
            Active = upgraded.Active,
            Action = "upgraded"
        }, "Upgraded.");
    }

    public async Task<ServiceResponse<CurrentPlanDto>> GetMyCurrentPlanAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<CurrentPlanDto>.Fail("Invalid user id.");

        var key = CacheKey_CurrentPlan(userId);
        if (_cache.TryGetValue(key, out CurrentPlanDto? cached) && cached is not null)
            return ServiceResponse<CurrentPlanDto>.Ok(cached);

        var active = await _subs.GetActiveByUserIdAsync(userId, ct);
        CurrentPlanDto dto;

        if (active is null || active.Plan is null)
        {
            dto = new CurrentPlanDto
            {
                PlanId = null,
                PlanName = null,
                PriceMonthly = null,
                MaxLinksPerMonth = null
            };
        }
        else
        {
            dto = new CurrentPlanDto
            {
                PlanId = active.Plan.Id,
                PlanName = active.Plan.Name,
                PriceMonthly = active.Plan.PriceMonthly,
                MaxLinksPerMonth = active.Plan.MaxLinksPerMonth
            };
        }

        _cache.Set(
            key,
            dto,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(45)));

        return ServiceResponse<CurrentPlanDto>.Ok(dto);
    }

    private void InvalidateForUser(Guid userId)
    {
        if (userId == Guid.Empty) return;

        _cache.Remove(CacheKey_ByUser(userId));
        _cache.Remove(CacheKey_Active(userId));
        _cache.Remove(CacheKey_CurrentPlan(userId));

        _profileService.InvalidateProfileCache(userId);
    }

    private static bool IsUpgrade(PlanDbTable current, PlanDbTable next)
    {
        if (next.PriceMonthly > current.PriceMonthly) return true;
        if (next.PriceMonthly < current.PriceMonthly) return false;

        if (next.MaxLinksPerMonth > current.MaxLinksPerMonth) return true;
        return false;
    }
}
