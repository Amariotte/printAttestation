using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System;

public class RouteNameFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Recherche l'attribut RouteName appliqué à l'action
        var routeNameAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<RouteNameAttribute>()
            .FirstOrDefault();

   

        if (routeNameAttribute != null)
        {
            string routeName = routeNameAttribute.RouteName;

            // Vous pouvez maintenant accéder au nom de la route et le loguer ou l'utiliser
            Console.WriteLine($"Nom de la route: {routeName}");

            // Ou vous pouvez l'ajouter au contexte si nécessaire
            context.HttpContext.Items["RouteDescription"] = routeName;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
