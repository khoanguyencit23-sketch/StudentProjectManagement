using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StudentProjectManagement.Application.Common.Interfaces;

namespace StudentProjectManagement.Infrastructure.Identity;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? Principal?.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value
                            ?? Principal?.FindFirst("email")?.Value;

    public string? Role => Principal?.FindFirst(ClaimTypes.Role)?.Value
                           ?? Principal?.FindFirst("role")?.Value;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
