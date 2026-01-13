using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Mappers;
using UrlShortener.BusinessLogic.Wrappers;
using UrlShortener.DataAccess.Repositories.Plan;
using UrlShortener.DataAccess.Repositories.Subscription;
using UrlShortener.DataAccess.Repositories.User;

namespace UrlShortener.BusinessLogic.Services.Subscription;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subs;
    private readonly IUserRepository _users;
    private readonly IPlanRepository _plans;

    public SubscriptionService(
        ISubscriptionRepository subs,
        IUserRepository users,
        IPlanRepository plans
    )
    {
        _subs = subs;
        _users = users;
        _plans = plans;
    }

    public async Task<ServiceResponse<List<SubscriptionDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _subs.GetAllAsync(ct);
        var dtos = items.Select(x => x.ToDto()).ToList();
        return ServiceResponse<List<SubscriptionDto>>.Ok(dtos);
    }

    public async Task<ServiceResponse<SubscriptionDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return ServiceResponse<SubscriptionDto>.Fail("Invalid id.");

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
            return ServiceResponse<SubscriptionDto>.Fail("Subscription not found.");

        return ServiceResponse<SubscriptionDto>.Ok(entity.ToDto());
    }

    public async Task<ServiceResponse<List<SubscriptionDto>>> GetByUserIdAsync(Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<List<SubscriptionDto>>.Fail("Invalid user id.");

        var items = await _subs.GetByUserIdAsync(userId, ct);
        var dtos = items.Select(x => x.ToDto()).ToList();
        return ServiceResponse<List<SubscriptionDto>>.Ok(dtos);
    }

    public async Task<ServiceResponse<SubscriptionDto>> GetActiveForUserAsync(Guid userId,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return ServiceResponse<SubscriptionDto>.Fail("Invalid user id.");

        var entity = await _subs.GetActiveForUserAsync(userId, ct);
        if (entity is null)
            return ServiceResponse<SubscriptionDto>.Fail("No active subscription.");

        return ServiceResponse<SubscriptionDto>.Ok(entity.ToDto());
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

        entity.UserId = dto.UserId;
        entity.PlanId = dto.PlanId;
        entity.Active = dto.Active;

        _subs.Update(entity);
        await _subs.SaveChangesAsync(ct);

        var updated = await _subs.GetByIdAsync(dto.Id, ct);
        return ServiceResponse<SubscriptionDto>.Ok(updated!.ToDto(), "Subscription updated.");
    }

    public async Task<ServiceResponse> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return ServiceResponse.Fail("Invalid id.");

        var entity = await _subs.GetByIdAsync(id, ct);
        if (entity is null)
            return ServiceResponse.Fail("Subscription not found.");

        _subs.Remove(entity);
        await _subs.SaveChangesAsync(ct);

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
}