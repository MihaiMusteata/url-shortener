using Microsoft.AspNetCore.Mvc;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Services.Auth;

namespace UrlShortener.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequestDto dto, CancellationToken ct)
    {
        var res = await _auth.SignUpAsync(dto, ct);
        if (!res.Success)
            return BadRequest(res.Message);

        return Ok(res.Data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var res = await _auth.LoginAsync(dto, ct);
        if (!res.Success)
            return BadRequest(res.Message);

        return Ok(res.Data);
    }
}