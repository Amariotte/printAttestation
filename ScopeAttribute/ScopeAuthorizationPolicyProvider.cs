using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace print_attestation.ScopeAttribute;

public class ScopeAuthorizationPolicyProvider
    : DefaultAuthorizationPolicyProvider
{
    public ScopeAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(
        string policyName)
    {
        var existingPolicy = await base.GetPolicyAsync(policyName);

        if (existingPolicy != null)
            return existingPolicy;

        // RequireScope
        if (policyName.StartsWith(
            "SCOPE:",
            StringComparison.OrdinalIgnoreCase))
        {
            var scope = policyName.Substring("SCOPE:".Length);

            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(
                    new ScopeRequirement(scope))
                .Build();
        }

        // RequireAnyScope
        if (policyName.StartsWith(
            "ANY_SCOPE:",
            StringComparison.OrdinalIgnoreCase))
        {
            var scopes = policyName
                .Substring("ANY_SCOPE:".Length)
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(
                    new ScopeRequirement(scopes))
                .Build();
        }

        return null;
    }
}