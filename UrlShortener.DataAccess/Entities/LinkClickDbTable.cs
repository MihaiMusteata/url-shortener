namespace UrlShortener.DataAccess.Entities;

public class LinkClickDbTable
{
    public Guid Id { get; set; }
    public Guid ShortLinkId { get; set; }
    public DateTime ClickedAt { get; set; }
    public string UserAgent { get; set; } = "";
    public string Referer { get; set; } = "";

    public ShortLinkDbTable? ShortLink { get; set; }
}