namespace UrlShortener.BusinessLogic.DTOs;

public class ShortLinkDto
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = "";
    public string ShortUrl { get; set; } = "";
    public string Alias { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool QrEnabled { get; set; }
    public long Clicks { get; set; }
}

public class ShortLinkCreateRequestDto
{
    public string Url { get; set; } = "";
    public string? CustomAlias { get; set; }
    public bool EnableQr { get; set; }
}

public class ShortLinkCreateResponseDto
{
    public Guid Id { get; set; }
    public string ShortUrl { get; set; } = "";
    public string Alias { get; set; } = "";
    public string? QrUrl { get; set; }
}