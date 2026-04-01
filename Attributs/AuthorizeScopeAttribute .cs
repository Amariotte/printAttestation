using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class AuthorizeScopeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _scope;

    public AuthorizeScopeAttribute(string scope)
    {
        _scope = scope;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.HasClaim("scope", _scope))
        {
            context.Result = new ForbidResult();
        }
    }
}