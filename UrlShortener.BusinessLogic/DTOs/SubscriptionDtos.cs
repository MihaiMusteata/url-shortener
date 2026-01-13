namespace UrlShortener.BusinessLogic.DTOs;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public bool Active { get; set; }

    public UserMinimalDto? User { get; set; }
    public string? PlanName { get; set; }
}

public class SubscriptionDetailsDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public bool Active { get; set; }
    
    public UserMinimalDto? User { get; set; }
    public string? PlanName { get; set; }
}

public class SubscriptionActionResultDto
{
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = "";
    public bool Active { get; set; }
    public string Action { get; set; } = "";
}