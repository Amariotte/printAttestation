using ask.ContextDb;
using ask.Model;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ask.Services
{
    /// <summary>
    /// Service de traçabilité pour les actions utilisateur et événements de connectivité
    /// </summary>
    public class TraceService
    {
        private readonly askContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TraceService> _logger;

        public TraceService(
            askContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TraceService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Trace une action utilisateur
        /// </summary>
        public async Task TraceActionAsync(
            TYPE_ACTION typeAction,
            object? details = null,
            int? userId = null,
            string? userEmail = null,
            string? description = null,
            int? statusCode = null,
            long? durationMs = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                // Récupérer l'utilisateur depuis les Items si disponible
                var user = httpContext?.Items.ContainsKey("User") == true
                    ? httpContext.Items["User"] as t_user
                    : null;

                var trace = new t_trace_action
                {
                    r_user_id = userId ?? user?.r_id,
                    r_user_email = userEmail ?? user?.r_email,
                    r_type_action = typeAction.ToString(),
                    r_details_json = details != null ? JsonSerializer.Serialize(details) : null,
                    r_ip_address = GetClientIpAddress(),
                    r_user_agent = httpContext?.Request.Headers["User-Agent"].ToString(),
                    r_http_method = httpContext?.Request.Method,
                    r_endpoint = httpContext?.Request.Path.ToString(),
                    r_status_code = statusCode ?? httpContext?.Response.StatusCode,
                    r_duration_ms = durationMs,
                    r_description = description,
                    r_created_at = DateTime.UtcNow
                };

                await _context.t_trace_action.AddAsync(trace);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Ne pas bloquer l'application si la traçabilité échoue
                _logger.LogError(ex, "Erreur lors de la traçabilité de l'action {TypeAction}", typeAction);
            }
        }

        /// <summary>
        /// Trace un événement de connectivité
        /// </summary>
        public async Task TraceConnexionAsync(
            TYPE_CONNEXION typeEvenement,
            bool succes,
            string? email = null,
            int? userId = null,
            string? raisonEchec = null,
            string? sessionTokenHash = null,
            DateTime? tokenExpiresAt = null,
            object? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                // Récupérer l'utilisateur depuis les Items si disponible
                var user = httpContext?.Items.ContainsKey("User") == true
                    ? httpContext.Items["User"] as t_user
                    : null;

                var trace = new t_trace_connexion
                {
                    r_user_id = userId ?? user?.r_id,
                    r_email = email ?? user?.r_email,
                    r_type_evenement = typeEvenement.ToString(),
                    r_succes = succes,
                    r_raison_echec = raisonEchec,
                    r_ip_address = GetClientIpAddress(),
                    r_user_agent = httpContext?.Request.Headers["User-Agent"].ToString(),
                    r_session_token_hash = sessionTokenHash,
                    r_token_expires_at = tokenExpiresAt,
                    r_details_json = details != null ? JsonSerializer.Serialize(details) : null,
                    r_created_at = DateTime.UtcNow
                };

                await _context.t_trace_connexion.AddAsync(trace);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Ne pas bloquer l'application si la traçabilité échoue
                _logger.LogError(ex, "Erreur lors de la traçabilité de la connexion {TypeEvenement}", typeEvenement);
            }
        }

        /// <summary>
        /// Récupère les actions récentes d'un utilisateur
        /// </summary>
        public async Task<List<t_trace_action>> GetUserActionsAsync(int userId, int limit = 50)
        {
            return await _context.t_trace_action
                .Where(t => t.r_user_id == userId)
                .OrderByDescending(t => t.r_created_at)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les événements de connexion récents d'un utilisateur
        /// </summary>
        public async Task<List<t_trace_connexion>> GetUserConnexionsAsync(int userId, int limit = 50)
        {
            return await _context.t_trace_connexion
                .Where(t => t.r_user_id == userId)
                .OrderByDescending(t => t.r_created_at)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les tentatives de connexion échouées récentes pour une IP
        /// </summary>
        public async Task<List<t_trace_connexion>> GetFailedLoginsByIpAsync(string ipAddress, int minutes = 15)
        {
            var since = DateTime.UtcNow.AddMinutes(-minutes);
            return await _context.t_trace_connexion
                .Where(t => t.r_ip_address == ipAddress
                    && !t.r_succes
                    && t.r_created_at >= since
                    && (t.r_type_evenement == TYPE_CONNEXION.CONNEXION_ECHOUEE_MOT_DE_PASSE.ToString()
                        || t.r_type_evenement == TYPE_CONNEXION.CONNEXION_ECHOUEE_COMPTE_INEXISTANT.ToString()))
                .OrderByDescending(t => t.r_created_at)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère l'adresse IP du client
        /// </summary>
        private string? GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Vérifier les headers de proxy
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                return ips[0].Trim();
            }

            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Génère un hash simple pour le token de session (pour privacy)
        /// </summary>
        public static string HashToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length < 10)
                return string.Empty;

            // Prendre les 8 premiers caractères du token
            var prefix = token.Substring(0, Math.Min(8, token.Length));

            // Calculer un hash simple
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            var hash = Convert.ToBase64String(hashBytes).Substring(0, 16);

            return $"{prefix}...{hash}";
        }
    }
}
