using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.BusinessLogic.Helpers;
using UrlShortener.BusinessLogic.Services.ShortLink;

namespace UrlShortener.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("")]
public class RedirectController : ControllerBase
{
    private readonly IShortLinkService _shortLinks;

    public RedirectController(IShortLinkService shortLinks)
    {
        _shortLinks = shortLinks;
    }

    [HttpGet("{alias}")]
    public async Task<IActionResult> Go([FromRoute] string alias, CancellationToken ct)
    {
        if (!UrlUtils.IsValidAlias(alias))
            return NotFound();

        var referrer = Request.Headers.Referer.ToString();
        var ua = Request.Headers.UserAgent.ToString();

        var ip =
            Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
            ?? HttpContext.Connection.RemoteIpAddress?.ToString();

        var res = await _shortLinks.ResolveAndTrackAsync(alias, referrer, ua, ip, ct);

        if (!res.Success)
        {
            return res.Message switch
            {
                "Link not found." => NotFound(),
                "Link inactive." => NotFound(),
                _ => BadRequest(res.Message)
            };
        }

        return Redirect(res.Data);
    }
}