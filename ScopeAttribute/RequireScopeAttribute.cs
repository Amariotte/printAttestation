using Microsoft.AspNetCore.Authorization;

namespace print_attestation.ScopeAttribute;

public class RequireScopeAttribute : AuthorizeAttribute
{
    public RequireScopeAttribute(string scope)
    {
        Policy = $"SCOPE:{scope}";
    }
}