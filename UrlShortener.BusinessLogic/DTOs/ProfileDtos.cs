namespace UrlShortener.BusinessLogic.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PlanName { get; set; } = "";
}

public class UsageSummaryDto
{
    public int LinksCreatedThisMonth { get; set; }
}

public class ProfilePageDto
{
    public UserProfileDto User { get; set; } = new();
    public PlanSummaryDto Plan { get; set; } = new();
    public UsageSummaryDto Usage { get; set; } = new();
    public List<ShortLinkDto> Links { get; set; } = new();
}