using System.Net;
using InteroperabiliteProject.ContextDb;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

public class JwtKeyloackMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly InteropContext _interopContext;
    private readonly ClientValidationService _clientValidationService;
    public JwtKeyloackMiddleware(RequestDelegate next, TokenValidationParameters tokenValidationParameters, InteropContext interopContext, ClientValidationService clientValidationService)
    {
        _next = next;
        _tokenValidationParameters = tokenValidationParameters;
        _interopContext = interopContext;
        _clientValidationService = clientValidationService;
    }

    public async Task InvokeAsync(HttpContext context)
    {

        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (token != null)
        {
            await AttachUserToContext(context, token);
        }

        await _next(context);
    }

    private async Task AttachUserToContext(HttpContext context, string token)
    {
        try
        {
            // Extraire le "code du client" du token
           var codeclient = context.User?.FindFirst("preferred_username")?.Value;
           await _clientValidationService.ValidateAndAttachClient(context, codeclient);
        }

        catch (SecurityTokenException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            var result = JsonConvert.SerializeObject(new
            {
                message = "Token invalide"
            });


            await context.Response.WriteAsync(result);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var result = JsonConvert.SerializeObject(new
            {
                message = "Erreur interne du serveur"
            });

            await context.Response.WriteAsync(result);
        }
    }
}
