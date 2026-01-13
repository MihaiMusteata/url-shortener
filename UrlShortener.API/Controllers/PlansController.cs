using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.BusinessLogic.Services.Plan;

namespace UrlShortener.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly IPlanService _planService;

    public PlansController(IPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var res = await _planService.GetAllAsync(ct);
        if (!res.Success)
            return BadRequest(res.Message);

        return Ok(res.Data);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var res = await _planService.GetByIdAsync(id, ct);
        if (!res.Success)
            return NotFound(res.Message);

        return Ok(res.Data);
    }
}
