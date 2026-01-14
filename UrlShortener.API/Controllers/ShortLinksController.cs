using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.BusinessLogic.Context;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Services.ShortLink;

namespace UrlShortener.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShortLinksController : ControllerBase
{
    private readonly IShortLinkService _shortLinks;
    private readonly ICurrentUserAccessor _current;

    public ShortLinksController(IShortLinkService shortLinks, ICurrentUserAccessor current)
    {
        _shortLinks = shortLinks;
        _current = current;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ShortLinkCreateRequestDto req, CancellationToken ct)
    {
        var userId = _current.GetUserId();
        var res = await _shortLinks.CreateAsync(userId, req, ct);

        if (!res.Success)
            return BadRequest(res.Message);

        return Ok(res.Data);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetails([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = _current.GetUserId();
        var res = await _shortLinks.GetDetailsAsync(userId, id, ct);

        if (!res.Success)
        {
            return res.Message switch
            {
                "Unauthorized." => Unauthorized(res.Message),
                "Forbidden." => Forbid(),
                "Link not found." => NotFound(res.Message),
                _ => BadRequest(res.Message)
            };
        }

        return Ok(res.Data);
    }
}
