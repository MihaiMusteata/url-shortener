namespace UrlShortener.DataAccess.Entities;

public class ShortLinkDbTable : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OriginalUrl { get; set; } = "";
    public string ShortCode { get; set; } = "";
    public bool IsActive { get; set; }
    public long TotalClicks { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserDbTable? User { get; set; }
    public QrCodeDbTable? QrCode { get; set; }
    public ICollection<LinkClickDbTable> LinkClicks { get; set; } = new List<LinkClickDbTable>();
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}

public class DailyClicksDto
{
    public string Date { get; set; } = "";
    public int Count { get; set; }
}

public class TopReferrerDto
{
    public string Referrer { get; set; } = "";
    public int Count { get; set; }
}

public class LinkClickEventDto
{
    public Guid Id { get; set; }
    public DateTime ClickedAt { get; set; }
    public string Referrer { get; set; } = "";
    public string Ua { get; set; } = "";
}

public class ShortLinkDetailsDto
{
    public Guid Id { get; set; }
    public string Alias { get; set; } = "";
    public string ShortUrl { get; set; } = "";
    public string OriginalUrl { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool QrEnabled { get; set; }
    public string? QrUrl { get; set; }

    public long TotalClicks { get; set; }
    public int UniqueReferrers { get; set; }

    public List<DailyClicksDto> ClicksLast7Days { get; set; } = new();
    public List<TopReferrerDto> TopReferrers { get; set; } = new();
    public List<LinkClickEventDto> RecentEvents { get; set; } = new();
}