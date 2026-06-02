using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    /// <summary>
    /// Session utilisateur avec tracking de connexion/déconnexion
    /// </summary>
    [Index(nameof(r_token_jti), Name = "IX_Session_TokenJti")]
    [Index(nameof(r_user_id_fk), Name = "IX_Session_UserId")]
    [Index(nameof(r_login_at), Name = "IX_Session_LoginAt")]
    [Index(nameof(r_is_active), Name = "IX_Session_IsActive")]
    public class t_session : t_base
    {
        /// <summary>
        /// JWT Token ID (JTI) de la session
        /// </summary>
        [MaxLength(100)]
        public string? r_token_jti { get; set; }

        /// <summary>
        /// Adresse IP du client
        /// </summary>
        [MaxLength(45)] // IPv6 max length
        public string? r_ip_address { get; set; }

        /// <summary>
        /// User-Agent du client (navigateur, application)
        /// </summary>
        [MaxLength(500)]
        public string? r_user_agent { get; set; }

        /// <summary>
        /// Date et heure de connexion (UTC)
        /// </summary>
        [Required(ErrorMessage = "La date de connexion est requise")]
        public DateTime r_login_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date et heure de déconnexion (UTC)
        /// </summary>
        public DateTime? r_logout_at { get; set; }

        /// <summary>
        /// Durée de la session en secondes
        /// </summary>
        [NotMapped]
        public int? DurationInSeconds
        {
            get
            {
                if (r_logout_at.HasValue)
                    return (int)(r_logout_at.Value - r_login_at).TotalSeconds;
                return null;
            }
        }

        /// <summary>
        /// Indique si la session est actuellement active
        /// </summary>
        public new bool r_is_active { get; set; } = true;

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
    }
}
