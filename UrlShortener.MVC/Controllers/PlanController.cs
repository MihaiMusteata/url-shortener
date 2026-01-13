using Microsoft.AspNetCore.Mvc;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Services.Plan;

namespace UrlShortener.MVC.Controllers;

public class PlanController : Controller
{
    private readonly IPlanService _planService;

    public PlanController(IPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var res = await _planService.GetAllAsync(ct);
        if (!res.Success)
        {
            TempData["Error"] = res.Message ?? "Failed to load plans.";
            return View(new List<PlanDto>());
        }

        return View(res.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var res = await _planService.GetByIdAsync(id, ct);
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.Message ?? "Plan not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(res.Data);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PlanDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlanDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var res = await _planService.CreateAsync(dto, ct);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError(string.Empty, res.Message ?? "Failed to create plan.");
            return View(dto);
        }

        TempData["Success"] = res.Message ?? "Plan created.";
        return RedirectToAction(nameof(Details), new { id = res.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var res = await _planService.GetByIdAsync(id, ct);
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.Message ?? "Plan not found.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new PlanDto
        {
            Id = res.Data.Id,
            Name = res.Data.Name,
            PriceMonthly = res.Data.PriceMonthly,
            MaxLinksPerMonth = res.Data.MaxLinksPerMonth,
            CustomAliasEnabled = res.Data.CustomAliasEnabled,
            QrEnabled = res.Data.QrEnabled
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PlanDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var res = await _planService.UpdateAsync(dto, ct);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError(string.Empty, res.Message ?? "Failed to update plan.");
            return View(dto);
        }

        TempData["Success"] = res.Message ?? "Plan updated.";
        return RedirectToAction(nameof(Details), new { id = res.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var res = await _planService.GetByIdAsync(id, ct);
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.Message ?? "Plan not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(res.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken ct)
    {
        var res = await _planService.DeleteAsync(id, ct);
        if (!res.Success)
        {
            TempData["Error"] = res.Message ?? "Failed to delete plan.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Success"] = res.Message ?? "Plan deleted.";
        return RedirectToAction(nameof(Index));
    }
}
