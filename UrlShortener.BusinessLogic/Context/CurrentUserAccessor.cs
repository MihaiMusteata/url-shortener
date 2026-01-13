using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace UrlShortener.BusinessLogic.Context;

public interface ICurrentUserAccessor
{
    Guid GetUserId();
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserAccessor(IHttpContextAccessor http) { _http = http; }

    public Guid GetUserId()
    {
        var user = _http.HttpContext?.User;
        if (user is null) return Guid.Empty;

        // folosește ce ai pus în JWT; de obicei ClaimTypes.NameIdentifier sau "sub"
        var idStr =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub") ??
            user.FindFirstValue("userId");

        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }
}