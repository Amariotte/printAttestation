using Microsoft.AspNetCore.Authorization;

namespace print_attestation.ScopeAttribute;

public class ScopeRequirement : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> Scopes { get; }

    public ScopeRequirement(params string[] scopes)
    {
        Scopes = scopes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}