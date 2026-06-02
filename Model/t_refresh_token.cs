using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    /// <summary>
    /// Token de rafraîchissement pour l'authentification JWT
    /// </summary>
    [Index(nameof(r_token), IsUnique = true, Name = "IX_RefreshToken_Token")]
    [Index(nameof(r_jti), Name = "IX_RefreshToken_Jti")]
    [Index(nameof(r_user_id_fk), Name = "IX_RefreshToken_UserId")]
    [Index(nameof(r_expires_at), Name = "IX_RefreshToken_ExpiresAt")]
    public class t_refresh_token : t_base
    {
        /// <summary>
        /// Token de rafraîchissement (unique, Base64)
        /// </summary>
        [Required(ErrorMessage = "Le token est requis")]
        [MaxLength(500)]
        public string r_token { get; set; } = string.Empty;

        /// <summary>
        /// JWT Token ID (JTI) associé
        /// </summary>
        [MaxLength(100)]
        public string? r_jti { get; set; }

        /// <summary>
        /// Date et heure d'expiration du token (UTC)
        /// </summary>
        [Required(ErrorMessage = "La date d'expiration est requise")]
        public DateTime r_expires_at { get; set; }

        /// <summary>
        /// Indique si le token a été révoqué
        /// </summary>
        public bool r_is_revoked { get; set; } = false;

        /// <summary>
        /// Date de révocation (UTC)
        /// </summary>
        public DateTime? r_revoked_at { get; set; }

        /// <summary>
        /// Token qui a remplacé celui-ci (en cas de rotation)
        /// </summary>
        [MaxLength(500)]
        public string? r_replaced_by { get; set; }

        /// <summary>
        /// Adresse IP du client lors de la création du token
        /// </summary>
        [MaxLength(45)] // IPv6 max length
        public string? r_ip_address { get; set; }

        /// <summary>
        /// User-Agent du client
        /// </summary>
        [MaxLength(500)]
        public string? r_user_agent { get; set; }

        /// <summary>
        /// Clé étrangère vers l'utilisateur
        /// </summary>
        [Required]
        [ForeignKey(nameof(r_userTab))]
        public int r_user_id_fk { get; set; }

        /// <summary>
        /// Relation de navigation vers l'utilisateur
        /// </summary>
        public t_user? r_userTab { get; set; }

        /// <summary>
        /// Vérifie si le token est expiré
        /// </summary>
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= r_expires_at;

        /// <summary>
        /// Vérifie si le token est actif (non révoqué et non expiré)
        /// </summary>
        [NotMapped]
        public bool IsActive => !r_is_revoked && !IsExpired && r_is_active && !r_is_delete;
    }
}
