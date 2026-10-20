using System.Security.Claims;

namespace CollectionManager.Api.Services;

internal static class ServiceHelpers
{
    public static int UserId(this ClaimsPrincipal user) => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new DomainException(401, "Валидната сесия липсва.");
    public static string Role(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role) ?? "";
    public static DomainException NotFound(string text = "Записът не е намерен.") => new(404, text);
    public static DomainException Forbidden(string text = "Нямате право за това действие.") => new(403, text);
    public static DomainException Conflict(string text) => new(409, text);
    public static DomainException BadRequest(string text) => new(400, text);
}

