using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SubscriptionService> _logger;

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
        IMemoryCache cache,
        ILogger<SubscriptionService> logger)
    {
        _subs = subs;
        _users = users;
        _plans = plans;
        _profileService = profileService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ServiceResponse<List<SubscriptionDetailsDto>>> GetAllAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey_All, out List<SubscriptionDetailsDto>? cached) && cached is not null)
        {
            _logger.LogDebug("Subscriptions GetAll cache hit.");
            return ServiceResponse<List<SubscriptionDetailsDto>>.Ok(cached);
        }

        _logger.LogInformation("Loading all subscriptions from database.");

        var items = await _subs.GetAllAsync(ct);
        var dtos = items.Select(x => x.ToDetailsDto()).ToList();

        _cache.Set(
            CacheKey_All,
            dtos,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(2)));

        _logger.LogInformation("Cached subscriptions list. Count={Count}", dtos.Count);

        return ServiceResponse<List<SubscriptionDetailsDto>>.Ok(dtos);
    }

    public async Task<ServiceResponse<SubscriptionDetailsDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("GetById failed: invalid id (empty).");
            return ServiceResponse<SubscriptionDetailsDto>.Fail("Invalid id.");
        }

        var key = CacheKey_ById(id);
        if (_cache.TryGetValue(key, out SubscriptionDetailsDto? cached) && cached is not null)
        {
            _logger.LogDebug("Subscription details cache hit. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse<SubscriptionDetailsDto>.Ok(cached);
        }

        _logger.LogInformation("Loading subscription from database. SubscriptionId={SubscriptionId}", id);

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
        {
            _logger.LogWarning("GetById failed: subscription not found. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse<SubscriptionDetailsDto>.Fail("Subscription not found.");
        }

        var dto = entity.ToDetailsDto();

        _cache.Set(
            key,
            dto,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(2)));

        _logger.LogInformation("Subscription cached. SubscriptionId={SubscriptionId}", id);

        return ServiceResponse<SubscriptionDetailsDto>.Ok(dto);
    }

    public async Task<ServiceResponse<List<SubscriptionDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("GetByUserId failed: invalid user id (empty).");
            return ServiceResponse<List<SubscriptionDto>>.Fail("Invalid user id.");
        }

        var key = CacheKey_ByUser(userId);
        if (_cache.TryGetValue(key, out List<SubscriptionDto>? cached) && cached is not null)
        {
            _logger.LogDebug("Subscriptions by user cache hit. UserId={UserId}, Count={Count}", userId, cached.Count);
            return ServiceResponse<List<SubscriptionDto>>.Ok(cached);
        }

        _logger.LogInformation("Loading subscriptions by user from database. UserId={UserId}", userId);

        var items = await _subs.GetByUserIdAsync(userId, ct);
        var dtos = items.Select(x => x.ToDto()).ToList();

        _cache.Set(
            key,
            dtos,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));

        _logger.LogInformation("Subscriptions by user cached. UserId={UserId}, Count={Count}", userId, dtos.Count);

        return ServiceResponse<List<SubscriptionDto>>.Ok(dtos);
    }

    public async Task<ServiceResponse<SubscriptionDto>> GetActiveForUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("GetActiveForUser failed: invalid user id (empty).");
            return ServiceResponse<SubscriptionDto>.Fail("Invalid user id.");
        }

        var key = CacheKey_Active(userId);
        if (_cache.TryGetValue(key, out SubscriptionDto? cached) && cached is not null)
        {
            _logger.LogDebug("Active subscription cache hit. UserId={UserId}", userId);
            return ServiceResponse<SubscriptionDto>.Ok(cached);
        }

        _logger.LogInformation("Loading active subscription from database. UserId={UserId}", userId);

        var entity = await _subs.GetActiveForUserAsync(userId, ct);
        if (entity is null)
        {
            _logger.LogWarning("No active subscription. UserId={UserId}", userId);
            return ServiceResponse<SubscriptionDto>.Fail("No active subscription.");
        }

        var dto = entity.ToDto();

        _cache.Set(
            key,
            dto,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(45)));

        _logger.LogInformation("Active subscription cached. UserId={UserId}, SubscriptionId={SubscriptionId}",
            userId, dto.Id);

        return ServiceResponse<SubscriptionDto>.Ok(dto);
    }

    public async Task<ServiceResponse<SubscriptionDto>> CreateAsync(SubscriptionDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating subscription. UserId={UserId}, PlanId={PlanId}, Active={Active}",
            dto.UserId, dto.PlanId, dto.Active);

        var v = await ValidateCreateOrUpdate(dto, ct);
        if (!v.Success)
        {
            _logger.LogWarning("Create subscription failed validation. UserId={UserId}, PlanId={PlanId}, Reason={Reason}",
                dto.UserId, dto.PlanId, v.Message);
            return ServiceResponse<SubscriptionDto>.Fail(v.Message ?? "Validation failed.");
        }

        var entity = dto.ToEntity();
        entity.Id = Guid.NewGuid();

        try
        {
            await _subs.AddAsync(entity, ct);
            await _subs.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription. UserId={UserId}, PlanId={PlanId}", dto.UserId, dto.PlanId);
            return ServiceResponse<SubscriptionDto>.Fail("Error creating subscription.");
        }

        var created = await _subs.GetByIdAsync(entity.Id, ct);

        InvalidateForUser(dto.UserId);
        _cache.Remove(CacheKey_All);

        _logger.LogInformation("Subscription created. SubscriptionId={SubscriptionId}, UserId={UserId}",
            entity.Id, dto.UserId);

        return ServiceResponse<SubscriptionDto>.Ok(created!.ToDto(), "Subscription created.");
    }

    public async Task<ServiceResponse<SubscriptionDto>> UpdateAsync(SubscriptionDto dto, CancellationToken ct = default)
    {
        if (dto.Id == Guid.Empty)
        {
            _logger.LogWarning("Update subscription failed: invalid id (empty).");
            return ServiceResponse<SubscriptionDto>.Fail("Invalid id.");
        }

        _logger.LogInformation("Updating subscription. SubscriptionId={SubscriptionId}", dto.Id);

        var v = await ValidateCreateOrUpdate(dto, ct);
        if (!v.Success)
        {
            _logger.LogWarning("Update subscription failed validation. SubscriptionId={SubscriptionId}, Reason={Reason}",
                dto.Id, v.Message);
            return ServiceResponse<SubscriptionDto>.Fail(v.Message ?? "Validation failed.");
        }

        var entity = await _subs.GetByIdAsync(dto.Id, ct);
        if (entity is null)
        {
            _logger.LogWarning("Update subscription failed: not found. SubscriptionId={SubscriptionId}", dto.Id);
            return ServiceResponse<SubscriptionDto>.Fail("Subscription not found.");
        }

        var oldUserId = entity.UserId;

        entity.UserId = dto.UserId;
        entity.PlanId = dto.PlanId;
        entity.Active = dto.Active;

        try
        {
            _subs.Update(entity);
            await _subs.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription. SubscriptionId={SubscriptionId}", dto.Id);
            return ServiceResponse<SubscriptionDto>.Fail("Error updating subscription.");
        }

        var updated = await _subs.GetByIdAsync(dto.Id, ct);

        _cache.Remove(CacheKey_All);
        _cache.Remove(CacheKey_ById(dto.Id));
        InvalidateForUser(oldUserId);
        InvalidateForUser(dto.UserId);

        _logger.LogInformation("Subscription updated. SubscriptionId={SubscriptionId}, OldUserId={OldUserId}, NewUserId={NewUserId}",
            dto.Id, oldUserId, dto.UserId);

        return ServiceResponse<SubscriptionDto>.Ok(updated!.ToDto(), "Subscription updated.");
    }

    public async Task<ServiceResponse> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("Delete subscription failed: invalid id (empty).");
            return ServiceResponse.Fail("Invalid id.");
        }

        _logger.LogInformation("Deleting subscription. SubscriptionId={SubscriptionId}", id);

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
        {
            _logger.LogWarning("Delete subscription failed: not found. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse.Fail("Subscription not found.");
        }

        var userId = entity.UserId;

        try
        {
            _subs.Remove(entity);
            await _subs.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse.Fail("Error deleting subscription.");
        }

        _cache.Remove(CacheKey_All);
        _cache.Remove(CacheKey_ById(id));
        InvalidateForUser(userId);

        _logger.LogInformation("Subscription deleted. SubscriptionId={SubscriptionId}, UserId={UserId}", id, userId);

        return ServiceResponse.Ok("Subscription deleted.");
    }

    public async Task<ServiceResponse> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("Activate subscription failed: invalid id (empty).");
            return ServiceResponse.Fail("Invalid id.");
        }

        _logger.LogInformation("Activating subscription. SubscriptionId={SubscriptionId}", id);

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
        {
            _logger.LogWarning("Activate subscription failed: not found. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse.Fail("Subscription not found.");
        }

        if (entity.Active)
        {
            _logger.LogInformation("Activate skipped: already active. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse.Ok("Subscription already active.");
        }

        entity.Active = true;

        try
        {
            _subs.Update(entity);
            await _subs.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating subscription. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse.Fail("Error activating subscription.");
        }

        _cache.Remove(CacheKey_All);
        _cache.Remove(CacheKey_ById(id));
        InvalidateForUser(entity.UserId);

        _logger.LogInformation("Subscription activated. SubscriptionId={SubscriptionId}, UserId={UserId}", id, entity.UserId);

        return ServiceResponse.Ok("Subscription activated.");
    }

    public async Task<ServiceResponse> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("Deactivate subscription failed: invalid id (empty).");
            return ServiceResponse.Fail("Invalid id.");
        }

        _logger.LogInformation("Deactivating subscription. SubscriptionId={SubscriptionId}", id);

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
        {
            _logger.LogWarning("Deactivate subscription failed: not found. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse.Fail("Subscription not found.");
        }

        if (!entity.Active)
        {
            _logger.LogInformation("Deactivate skipped: already inactive. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse.Ok("Subscription already inactive.");
        }

        entity.Active = false;

        try
        {
            _subs.Update(entity);
            await _subs.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating subscription. SubscriptionId={SubscriptionId}", id);
            return ServiceResponse.Fail("Error deactivating subscription.");
        }

        _cache.Remove(CacheKey_All);
        _cache.Remove(CacheKey_ById(id));
        InvalidateForUser(entity.UserId);

        _logger.LogInformation("Subscription deactivated. SubscriptionId={SubscriptionId}, UserId={UserId}", id, entity.UserId);

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
        _logger.LogInformation("Subscribe requested. UserId={UserId}, PlanId={PlanId}", userId, planId);

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            _logger.LogWarning("Subscribe failed: user not found. UserId={UserId}", userId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("User not found.");
        }

        var plan = await _plans.GetByIdAsync(planId, ct);
        if (plan is null)
        {
            _logger.LogWarning("Subscribe failed: plan not found. PlanId={PlanId}, UserId={UserId}", planId, userId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Plan not found.");
        }

        var active = await _subs.GetActiveByUserIdAsync(userId, ct);
        if (active is not null)
        {
            _logger.LogWarning("Subscribe blocked: user already has active subscription. UserId={UserId}", userId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail(
                "User already has an active subscription. Use upgrade.");
        }

        var sub = new SubscriptionDbTable
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            Active = true
        };

        try
        {
            await _subs.AddAsync(sub, ct);
            await _subs.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing. UserId={UserId}, PlanId={PlanId}", userId, planId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Error subscribing.");
        }

        _cache.Remove(CacheKey_All);
        InvalidateForUser(userId);

        _logger.LogInformation("Subscribed successfully. UserId={UserId}, SubscriptionId={SubscriptionId}, PlanId={PlanId}",
            userId, sub.Id, planId);

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
        _logger.LogInformation("Upgrade requested. UserId={UserId}, NewPlanId={NewPlanId}", userId, newPlanId);

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            _logger.LogWarning("Upgrade failed: user not found. UserId={UserId}", userId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("User not found.");
        }

        var newPlan = await _plans.GetByIdAsync(newPlanId, ct);
        if (newPlan is null)
        {
            _logger.LogWarning("Upgrade failed: plan not found. NewPlanId={NewPlanId}, UserId={UserId}", newPlanId, userId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Plan not found.");
        }

        var current = await _subs.GetActiveByUserIdAsync(userId, ct);
        if (current is null)
        {
            _logger.LogWarning("Upgrade failed: no active subscription. UserId={UserId}", userId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("No active subscription found.");
        }

        if (current.PlanId == newPlanId)
        {
            _logger.LogWarning("Upgrade blocked: already on this plan. UserId={UserId}, PlanId={PlanId}", userId, newPlanId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("User is already on this plan.");
        }

        if (current.Plan is null)
        {
            _logger.LogError("Upgrade failed: current plan data missing. UserId={UserId}, CurrentSubId={SubId}", userId, current.Id);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Current plan data missing.");
        }

        if (!IsUpgrade(current.Plan, newPlan))
        {
            _logger.LogWarning("Upgrade blocked: downgrade not allowed. UserId={UserId}, CurrentPlanId={CurrentPlanId}, NewPlanId={NewPlanId}",
                userId, current.PlanId, newPlanId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Only upgrades are allowed.");
        }

        current.Active = false;
        _subs.Update(current);

        var upgraded = new SubscriptionDbTable
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = newPlanId,
            Active = true
        };

        try
        {
            await _subs.AddAsync(upgraded, ct);
            await _subs.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription. UserId={UserId}, NewPlanId={NewPlanId}", userId, newPlanId);
            return ServiceResponse<SubscriptionActionResultDto>.Fail("Error upgrading subscription.");
        }

        _cache.Remove(CacheKey_All);
        InvalidateForUser(userId);

        _logger.LogInformation("Upgrade successful. UserId={UserId}, OldSubId={OldSubId}, NewSubId={NewSubId}, NewPlanId={NewPlanId}",
            userId, current.Id, upgraded.Id, newPlanId);

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
        {
            _logger.LogWarning("GetMyCurrentPlan failed: invalid user id (empty).");
            return ServiceResponse<CurrentPlanDto>.Fail("Invalid user id.");
        }

        var key = CacheKey_CurrentPlan(userId);
        if (_cache.TryGetValue(key, out CurrentPlanDto? cached) && cached is not null)
        {
            _logger.LogDebug("Current plan cache hit. UserId={UserId}", userId);
            return ServiceResponse<CurrentPlanDto>.Ok(cached);
        }

        _logger.LogInformation("Loading current plan from database. UserId={UserId}", userId);

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

        _logger.LogInformation("Current plan cached. UserId={UserId}, PlanId={PlanId}, PlanName={PlanName}",
            userId, dto.PlanId, dto.PlanName);

        return ServiceResponse<CurrentPlanDto>.Ok(dto);
    }

    private void InvalidateForUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogDebug("InvalidateForUser ignored: empty userId.");
            return;
        }

        _cache.Remove(CacheKey_ByUser(userId));
        _cache.Remove(CacheKey_Active(userId));
        _cache.Remove(CacheKey_CurrentPlan(userId));

        _profileService.InvalidateProfileCache(userId);

        _logger.LogDebug("Subscription-related caches invalidated for user. UserId={UserId}", userId);
    }

    private static bool IsUpgrade(PlanDbTable current, PlanDbTable next)
    {
        if (next.PriceMonthly > current.PriceMonthly) return true;
        if (next.PriceMonthly < current.PriceMonthly) return false;

        if (next.MaxLinksPerMonth > current.MaxLinksPerMonth) return true;
        return false;
    }
}
