using System.Security.Claims;


namespace print_attestation.Security
{

    public static class ClaimsPrincipalExtensions
    {
        public static bool HasScope(
            this ClaimsPrincipal user,
            string scope)
        {
            return user.Claims
                .Where(c => c.Type == "scope")
                .SelectMany(c => c.Value.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                ))
                .Contains(
                    scope,
                    StringComparer.OrdinalIgnoreCase
                );
        }
    }
}
