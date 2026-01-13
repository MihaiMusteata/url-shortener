namespace UrlShortener.DataAccess.Entities;

public enum UserRole { user, admin }

public class UserDbTable
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Username { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public UserRole Role { get; set; }

    public ICollection<ShortLinkDbTable> ShortLinks { get; set; } = new List<ShortLinkDbTable>();
    public ICollection<SubscriptionDbTable> Subscriptions { get; set; } = new List<SubscriptionDbTable>();
}