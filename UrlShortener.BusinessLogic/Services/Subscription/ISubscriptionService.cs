using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Wrappers;

namespace UrlShortener.BusinessLogic.Services.Subscription;

public interface ISubscriptionService
{
    Task<ServiceResponse<List<SubscriptionDetailsDto>>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceResponse<SubscriptionDetailsDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResponse<SubscriptionDto>> CreateAsync(SubscriptionDto subscriptionDto, CancellationToken ct = default);
    Task<ServiceResponse<SubscriptionDto>> UpdateAsync(SubscriptionDto subscriptionDto, CancellationToken ct = default);
    Task<ServiceResponse<List<SubscriptionDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<ServiceResponse<SubscriptionDto>> GetActiveForUserAsync(Guid userId, CancellationToken ct = default);
    Task<ServiceResponse> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResponse> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResponse> DeactivateAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResponse<SubscriptionActionResultDto>> SubscribeAsync(Guid userId, Guid planId, CancellationToken ct = default);
    Task<ServiceResponse<SubscriptionActionResultDto>> UpgradeAsync(Guid userId, Guid newPlanId, CancellationToken ct = default);
    Task<ServiceResponse<CurrentPlanDto>> GetMyCurrentPlanAsync(Guid userId, CancellationToken ct = default);

}