using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ask.Dtos.General;
using ask.ContextDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

public class JwtSecureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtSecureMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public JwtSecureMiddleware(
        RequestDelegate next,
        ILogger<JwtSecureMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            bool isProtected = false;
            var endpoint = context.GetEndpoint();
            if (endpoint is not null)
            {
                var cad = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                if (cad != null)
                {
                    isProtected =
                        cad.MethodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any() ||
                        cad.ControllerTypeInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any();
                }
            }

            if (!isProtected)
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            var token = authHeader?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).Last();

            if (string.IsNullOrWhiteSpace(token))
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                    GeneraleRetour.BuildUnauthorized(
                        detail: "Le token d'authentification est requis",
                        instance: context.Request.Path,
                        invalidParams: new List<InvalidParam> {
                            new InvalidParam { name = "Authorization", reason = "Le token d'authentification est requis" }
                        }));
                return;
            }

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Key"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogError("JwtSettings:Key manquant dans la configuration");
                await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                    GeneraleRetour.BuildProblemResponse500(instance: context.Request.Path));
                return;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            ClaimsPrincipal principal;
            try
            {
                principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);
            }
            catch (SecurityTokenExpiredException)
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                    GeneraleRetour.BuildUnauthorized(
                        detail: "Token expiré",
                        instance: context.Request.Path));
                return;
            }
            catch (SecurityTokenException)
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                    GeneraleRetour.BuildUnauthorized(
                        detail: "Token invalide",
                        instance: context.Request.Path));
                return;
            }

            context.User = principal;

            var idUserClaim = principal.FindFirst("iduser")?.Value;
            if (string.IsNullOrEmpty(idUserClaim) || !int.TryParse(idUserClaim, out var userId))
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                    GeneraleRetour.BuildUnauthorized(
                        detail: "Données obligatoires manquantes dans le token.",
                        instance: context.Request.Path));
                return;
            }

            var dbFactory = context.RequestServices.GetRequiredService<IDbContextFactory<askContext>>();
            await using var db = await dbFactory.CreateDbContextAsync(context.RequestAborted);

            var user = await db.t_user
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.r_id == userId && u.r_is_delete != true, context.RequestAborted);

            if (user == null)
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                    GeneraleRetour.BuildUnauthorized(
                        detail: "Utilisateur introuvable ou désactivé.",
                        instance: context.Request.Path));
                return;
            }

            context.Items["User"] = user;

            await _next(context);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token error");
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                GeneraleRetour.BuildUnauthorized(
                    detail: "Token invalide",
                    instance: context.Request.Path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in JwtSecureMiddleware");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                GeneraleRetour.BuildProblemResponse500(
                    detail: "Une erreur est survenue lors de la vérification du token",
                    instance: context.Request.Path));
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, object problem)
    {
        if (context.Response.HasStarted) return;

        if (statusCode == StatusCodes.Status204NoContent)
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(problem));
    }
}
