using print_attestation.Dtos.General;

public class ForbiddenMiddleware
{
    private readonly RequestDelegate _next;

    public ForbiddenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status403Forbidden
            && !context.Response.HasStarted)
        {
            context.Response.ContentType = "application/json";

            var response = GeneraleRetour.BuildForbid(
                detail: "Vous n'avez pas les droits nécessaires pour effectuer cette action.",
                instance: context.Request.Path,
                invalidParams: new List<InvalidParam>
                {
                    new InvalidParam
                    {
                        name = "Forbidden",
                        reason = "Vous ne disposez pas du scope requis pour effectuer cette action."
                    }
                }
            );

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
