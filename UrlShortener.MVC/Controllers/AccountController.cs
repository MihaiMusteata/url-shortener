using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.BusinessLogic.Services.Auth;
using UrlShortener.MVC.Models;

namespace UrlShortener.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _auth.LoginAsync(new LoginRequestDto
        {
            Email = model.Email,
            Password = model.Password
        }, ct);

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Login failed.");
            return View(model);
        }

        SetJwtCookie(result.Data.AccessToken, result.Data.ExpiresAtUtc);

        await SignInCookiePrincipal(result.Data.AccessToken);

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _auth.SignUpAsync(new SignUpRequestDto
        {
            Email = model.Email,
            Username = model.Username,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Password = model.Password
        }, ct);

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Registration failed.");
            return View(model);
        }

        SetJwtCookie(result.Data.AccessToken, result.Data.ExpiresAtUtc);
        await SignInCookiePrincipal(result.Data.AccessToken);

        return RedirectToLocal(model.ReturnUrl);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete(JwtCookieName);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }


    private const string JwtCookieName = "access_token";

    private void SetJwtCookie(string token, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(JwtCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAtUtc
        });
    }

    private async Task SignInCookiePrincipal(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        var identity = new System.Security.Claims.ClaimsIdentity(
            token.Claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = token.ValidTo
            });
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}
