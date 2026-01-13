using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.BusinessLogic.Services.Subscription;

namespace UrlShortener.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subs;

    public SubscriptionsController(ISubscriptionService subs)
    {
        _subs = subs;
    }

    public class SubscribeRequest { public Guid PlanId { get; set; } }
    public class UpgradeRequest { public Guid NewPlanId { get; set; } }

    [HttpGet("me/current")]
    public async Task<IActionResult> GetMyCurrentPlan(CancellationToken ct)
    {
        var userId = GetUserIdFromJwt();
        if (userId is null)
            return Unauthorized("Invalid token.");

        var res = await _subs.GetMyCurrentPlanAsync(userId.Value, ct);
        if (!res.Success)
            return BadRequest(res.Message);

        return Ok(res.Data);
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req, CancellationToken ct)
    {
        var userId = GetUserIdFromJwt();
        if (userId is null)
            return Unauthorized("Invalid token.");

        var res = await _subs.SubscribeAsync(userId.Value, req.PlanId, ct);
        if (!res.Success)
            return BadRequest(res.Message);

        return Ok(res.Data);
    }

    [HttpPost("upgrade")]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeRequest req, CancellationToken ct)
    {
        var userId = GetUserIdFromJwt();
        if (userId is null)
            return Unauthorized("Invalid token.");

        var res = await _subs.UpgradeAsync(userId.Value, req.NewPlanId, ct);
        if (!res.Success)
            return BadRequest(res.Message);

        return Ok(res.Data);
    }

    private Guid? GetUserIdFromJwt()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var id))
            return id;
        return null;
    }
}