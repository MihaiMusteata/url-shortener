using Microsoft.AspNetCore.Mvc;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Services.Subscription;

namespace UrlShortener.MVC.Controllers;

public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var res = await _subscriptionService.GetAllAsync(ct);
        if (!res.Success)
        {
            TempData["Error"] = res.Message ?? "Failed to load subscriptions.";
            return View(new List<SubscriptionDto>());
        }

        return View(res.Data ?? new List<SubscriptionDto>());
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var res = await _subscriptionService.GetByIdAsync(id, ct);
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.Message ?? "Subscription not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(res.Data);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new SubscriptionDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubscriptionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var res = await _subscriptionService.CreateAsync(dto, ct);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError(string.Empty, res.Message ?? "Failed to create subscription.");
            return View(dto);
        }

        TempData["Success"] = res.Message ?? "Subscription created.";
        return RedirectToAction(nameof(Details), new { id = res.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var res = await _subscriptionService.GetByIdAsync(id, ct);
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.Message ?? "Subscription not found.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new SubscriptionDto
        {
            Id = res.Data.Id,
            UserId = res.Data.UserId,
            PlanId = res.Data.PlanId,
            Active = res.Data.Active
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SubscriptionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var res = await _subscriptionService.UpdateAsync(dto, ct);
        if (!res.Success || res.Data is null)
        {
            ModelState.AddModelError(string.Empty, res.Message ?? "Failed to update subscription.");
            return View(dto);
        }

        TempData["Success"] = res.Message ?? "Subscription updated.";
        return RedirectToAction(nameof(Details), new { id = res.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var res = await _subscriptionService.GetByIdAsync(id, ct);
        if (!res.Success || res.Data is null)
        {
            TempData["Error"] = res.Message ?? "Subscription not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(res.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken ct)
    {
        var res = await _subscriptionService.DeleteAsync(id, ct);
        if (!res.Success)
        {
            TempData["Error"] = res.Message ?? "Failed to delete subscription.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Success"] = res.Message ?? "Subscription deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var res = await _subscriptionService.ActivateAsync(id, ct);
        TempData[res.Success ? "Success" : "Error"] = res.Message ?? (res.Success ? "Activated." : "Failed.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var res = await _subscriptionService.DeactivateAsync(id, ct);
        TempData[res.Success ? "Success" : "Error"] = res.Message ?? (res.Success ? "Deactivated." : "Failed.");
        return RedirectToAction(nameof(Details), new { id });
    }
}