using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.BusinessLogic.Services.Profile;

namespace UrlShortener.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profile;

    public ProfileController(IProfileService profile)
    {
        _profile = profile;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = GetUserIdFromJwt();
        if (userId is null)
            return Unauthorized("Invalid token.");

        var res = await _profile.GetMyProfileAsync(userId.Value, ct);
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