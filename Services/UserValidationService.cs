using System.Text;
using ask.ContextDb;
using ask.Dtos.General; // <- pour GeneraleRetour / ProblemsDetails
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

public class UserValidationService
{
    private readonly IDbContextFactory<askContext> _contextFactory;
    private readonly ILogger<UserValidationService> _logger;

    private const string HttpItemsClientKey = "Client";

    public UserValidationService(
        IDbContextFactory<askContext> contextFactory,
        ILogger<UserValidationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<bool> ValidateAndAttachClient(HttpContext httpContext, int userID)
    {
        if (userID <= 0 )
        {
            await WriteUnauthorizedAsync(httpContext, "L'identifiant du client est manquant ou vide.");
            return false;
        }

        try
        {
            await using var db = await _contextFactory.CreateDbContextAsync(httpContext.RequestAborted);

            var user = await db.t_user
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.r_id == userID && p.r_is_delete != true,
                    httpContext.RequestAborted);

            if (user is null)
            {
                await WriteUnauthorizedAsync(httpContext, "Utilisateur introuvable ou désactivé.");
                return false;
            }

            // Stocke l'entité (ou un DTO si tu préfères) dans le contexte de la requête
            httpContext.Items[HttpItemsClientKey] = user;
            return true;
        }
        catch (OperationCanceledException)
        {
            // Annulation propagée (gestion globale possible pour 499)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client validation failed for userID {Code}", userID);
            await WriteServerErrorAsync(httpContext, "Erreur lors de la validation du client.");
            return false;
        }
    }

    private static async Task WriteUnauthorizedAsync(HttpContext ctx, string? detail = null)
    {
        if (ctx.Response.HasStarted) return;

        var problem = GeneraleRetour.BuildUnauthorized(
            detail ?? "Vous n'êtes pas autorisé à accéder à cette ressource.",
            ctx.Request?.Path.Value ?? string.Empty
        );

        await WriteProblemAsync(ctx, problem);
    }

    private static async Task WriteServerErrorAsync(HttpContext ctx, string? detail = null)
    {
        if (ctx.Response.HasStarted) return;

        var problem = GeneraleRetour.BuildProblemResponse500(
            detail ?? "Une erreur interne s’est produite lors du traitement de la demande.",
            ctx.Request?.Path.Value ?? string.Empty
        );

        await WriteProblemAsync(ctx, problem);
    }

    /// <summary>
    /// Écrit un payload RFC 7807 à partir d'un ProblemsDetails (issu de tes builders).
    /// </summary>
    private static async Task WriteProblemAsync(HttpContext ctx, ProblemsDetails problem, CancellationToken ct = default)
    {
        var statusCode = problem?.status > 0 ? problem.status : StatusCodes.Status500InternalServerError;

        // 204 => pas de corps
        if (statusCode == StatusCodes.Status204NoContent)
        {
            ctx.Response.Clear();
            ctx.Response.StatusCode = statusCode;
            await ctx.Response.CompleteAsync();
            return;
        }

        var payload = JsonConvert.SerializeObject(problem);
        var bytes = Encoding.UTF8.GetBytes(payload);

        ctx.Response.Clear();
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/problem+json; charset=utf-8";
        ctx.Response.ContentLength = bytes.Length;

        var token = ct.CanBeCanceled ? ct : ctx.RequestAborted;
        await ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length, token);
        await ctx.Response.Body.FlushAsync(token);
        await ctx.Response.CompleteAsync();
    }
}
