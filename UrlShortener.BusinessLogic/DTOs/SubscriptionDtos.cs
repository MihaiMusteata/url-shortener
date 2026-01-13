namespace UrlShortener.BusinessLogic.DTOs;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public bool Active { get; set; }
}
