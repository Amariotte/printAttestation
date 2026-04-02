using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using ask.Dtos.General;
using ask.Dtos.RequestToSendDto;
using InteroperabiliteProject.ContextDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

public class JwtSecureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityConfig _securityConfig;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JwtSecureMiddleware> _logger;

    public JwtSecureMiddleware(
        RequestDelegate next,
        ILogger<JwtSecureMiddleware> logger,
        IOptions<SecurityConfig> securityConfig,
        IHttpClientFactory httpClientFactory) 
    {
        _next = next;
        _securityConfig = securityConfig.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            //Déterminer si l'endpoint est protégé (Authorize sur action ou contrôleur)
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

            // Endpoints protégés
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

            var clientValidation = context.RequestServices.GetRequiredService<ClientValidationService>();

            // ☑️ Validation externe du token (client créé par requête, pas d’en-têtes persistants)
            if (!await ValidateTokenExternally(token, context))
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                    GeneraleRetour.BuildUnauthorized(
                        detail: "Token invalide ou expiré.",
                        instance: context.Request.Path,
                        invalidParams: new List<InvalidParam> {
                            new InvalidParam { name = "Authorization", reason = "Token invalide ou expiré." }
                        }));
                return;
            }

            // Parser les claims et attacher l'utilisateur
            var raw = new JwtSecurityToken(token);
            if (!AuthenticateUser(context, raw, out var codeClient))
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                    GeneraleRetour.BuildUnauthorized(
                        detail: "Données obligatoires manquantes dans le token.",
                        instance: context.Request.Path));
                return;
            }

            // Vérification/attachement client (peut enrichir HttpContext)
            await clientValidation.ValidateAndAttachClient(context, codeClient);

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

    private bool AuthenticateUser(HttpContext context, JwtSecurityToken token, out string? codeClient)
    {
        _logger.LogInformation("AuthenticateUser => début");

        var username = token.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
        var racine = token.Claims.FirstOrDefault(c => c.Type == "racine")?.Value;
        var isAdminC = token.Claims.FirstOrDefault(c => c.Type == "isadmin")?.Value;

        codeClient = racine;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(racine))
            return false;

        bool isAdmin = isAdminC == "1";
        var role = isAdmin ? "Admin" : "User";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("racine", racine),
            new Claim("isadmin", isAdmin.ToString())
        };

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        _logger.LogInformation("AuthenticateUser => fin");
        return true;
    }

    private async Task<bool> ValidateTokenExternally(string token, HttpContext context)
    {
        try
        {
            _logger.LogInformation("Vérification de la validité du token (externe)");

            string baseUrl = _securityConfig.Secure.Uri.TrimEnd('/');
            string url = $"{baseUrl}/api/secure/client/veriftokken";

            var client = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(req, context.RequestAborted);

            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync(context.RequestAborted);
            var validation = JsonConvert.DeserializeObject<ValidationResponse>(json) ?? new ValidationResponse { result = false };
            _logger.LogInformation("Token validité retour {Result}", validation.result);
            return validation.result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur lors de la validation externe du token");
            return false;
        }
    }

   
    private static async Task WriteProblemAsync(HttpContext context, int statusCode, object problem)
    {
        // Si la réponse a démarré, on ne touche à rien
        if (context.Response.HasStarted) return;

        // Ne jamais écrire un body avec 204
        if (statusCode == StatusCodes.Status204NoContent)
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(problem));
    }

    private class ValidationResponse
    {
        public bool result { get; set; }
        public string description { get; set; }
    }
}
