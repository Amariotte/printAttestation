using Microsoft.AspNetCore.Authorization;

namespace print_attestation.ScopeAttribute;

public class RequireAnyScopeAttribute : AuthorizeAttribute
{
    public RequireAnyScopeAttribute(params string[] scopes)
    {
        if (scopes == null || scopes.Length == 0)
            throw new ArgumentException("Au moins un scope est requis.");

        Policy = $"ANY_SCOPE:{string.Join(",", scopes)}";
    }
}