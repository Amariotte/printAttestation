using Microsoft.AspNetCore.Authorization;

namespace print_attestation.ScopeAttribute;

public class ScopeAuthorizationHandler :
    AuthorizationHandler<ScopeRequirement>,
    IAuthorizationHandler
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        var scopeClaim = context.User.FindFirst("scope");

        if (scopeClaim == null || string.IsNullOrWhiteSpace(scopeClaim.Value))
            return Task.CompletedTask;

        var userScopes = scopeClaim.Value
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // RequireScope
        if (requirement.Scopes.Count == 1)
        {
            if (userScopes.Contains(requirement.Scopes.First()))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        // RequireAnyScope
        if (requirement.Scopes.Any(userScopes.Contains))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}