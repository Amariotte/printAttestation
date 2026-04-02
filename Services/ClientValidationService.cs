using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using InteroperabiliteProject.ContextDb;
using ask.Dtos.General; // <- pour GeneraleRetour / ProblemsDetails

public class ClientValidationService
{
    private readonly IDbContextFactory<InteropContext> _contextFactory;
    private readonly ILogger<ClientValidationService> _logger;

    private const string HttpItemsClientKey = "Client";

    public ClientValidationService(
        IDbContextFactory<InteropContext> contextFactory,
        ILogger<ClientValidationService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<bool> ValidateAndAttachClient(HttpContext httpContext, string codeclient)
    {
        if (string.IsNullOrWhiteSpace(codeclient))
        {
            await WriteUnauthorizedAsync(httpContext, "Code client manquant ou vide.");
            return false;
        }

        try
        {
            await using var db = await _contextFactory.CreateDbContextAsync(httpContext.RequestAborted);

            var client = await db.t_client
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.code == codeclient && p.is_delete != true,
                    httpContext.RequestAborted);

            if (client is null)
            {
                await WriteUnauthorizedAsync(httpContext, "Client introuvable ou désactivé.");
                return false;
            }

            // Stocke l'entité (ou un DTO si tu préfères) dans le contexte de la requête
            httpContext.Items[HttpItemsClientKey] = client;
            return true;
        }
        catch (OperationCanceledException)
        {
            // Annulation propagée (gestion globale possible pour 499)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client validation failed for codeclient {Code}", codeclient);
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
