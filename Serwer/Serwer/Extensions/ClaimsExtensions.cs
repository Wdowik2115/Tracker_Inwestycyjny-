using System.Security.Claims;

namespace Serwer.Extensions
{
    public static class ClaimsExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub")
                ?? throw new InvalidOperationException("UserId claim not found.");
            return Guid.Parse(value);
        }
    }
}
