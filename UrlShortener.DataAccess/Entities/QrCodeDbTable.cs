namespace UrlShortener.DataAccess.Entities;

public class QrCodeDbTable
{
    public Guid Id { get; set; }
    public Guid ShortLinkId { get; set; }
    public string Format { get; set; } = "png";
    public string FileUrl { get; set; } = "";

    public ShortLinkDbTable? ShortLink { get; set; }
}