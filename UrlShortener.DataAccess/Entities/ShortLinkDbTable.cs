namespace UrlShortener.DataAccess.Entities;

public class ShortLinkDbTable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OriginalUrl { get; set; } = "";
    public string ShortCode { get; set; } = "";
    public bool IsActive { get; set; }
    public long TotalClicks { get; set; }

    public UserDbTable? User { get; set; }
    public QrCodeDbTable? QrCode { get; set; }
    public ICollection<LinkClickDbTable> LinkClicks { get; set; } = new List<LinkClickDbTable>();
}