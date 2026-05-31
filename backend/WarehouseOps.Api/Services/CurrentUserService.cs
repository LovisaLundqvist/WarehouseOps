using System.Security.Claims;
using WarehouseOps.Application.Interfaces;

namespace WarehouseOps.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentUserDisplayName()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return "System";
        }

        var displayName = user.FindFirst("displayName")?.Value?.Trim();

        var email =
            user.FindFirst(ClaimTypes.Email)?.Value?.Trim()
            ?? user.FindFirst("email")?.Value?.Trim()
            ?? user.FindFirst(ClaimTypes.Name)?.Value?.Trim()
            ?? user.Identity.Name?.Trim();

        if (!string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(email))
        {
            return $"{displayName} ({email})";
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return email;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return "System";
    }
}
