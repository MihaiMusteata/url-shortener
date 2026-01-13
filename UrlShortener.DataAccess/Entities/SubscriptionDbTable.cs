namespace UrlShortener.DataAccess.Entities;

public class SubscriptionDbTable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public bool Active { get; set; }

    public UserDbTable? User { get; set; }
    public PlanDbTable? Plan { get; set; }
}