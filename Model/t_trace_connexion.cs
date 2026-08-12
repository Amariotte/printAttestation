using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    /// <summary>
    /// Modèle de traçabilité des événements de connectivité
    /// </summary>
    [Table("t_trace_connexion")]
    public class t_trace_connexion : t_base
    {
        

        /// <summary>
        /// ID de l'utilisateur (nullable pour tentatives échouées)
        /// </summary>
        public int? r_user_id { get; set; }

        /// <summary>
        /// Email/identifiant de connexion (dénormalisé pour historique)
        /// </summary>
        [MaxLength(255)]
        public string? r_email { get; set; }

        /// <summary>
        /// Type d'événement de connexion (voir enum TYPE_CONNEXION)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string r_type_evenement { get; set; } = string.Empty;

        /// <summary>
        /// Succès ou échec de la tentative
        /// </summary>
        [Required]
        public bool r_succes { get; set; }

        /// <summary>
        /// Raison de l'échec (ex: "Mot de passe incorrect", "Compte bloqué")
        /// </summary>
        [MaxLength(500)]
        public string? r_raison_echec { get; set; }

        /// <summary>
        /// Adresse IP du client
        /// </summary>
        [MaxLength(45)] // IPv6
        public string? r_ip_address { get; set; }

        /// <summary>
        /// User-Agent du navigateur/client
        /// </summary>
        [MaxLength(500)]
        public string? r_user_agent { get; set; }

        /// <summary>
        /// Token de session (hash, pour traçabilité)
        /// </summary>
        [MaxLength(100)]
        public string? r_session_token_hash { get; set; }

      
        /// <summary>
        /// Informations supplémentaires au format JSON
        /// </summary>
        [Column(TypeName = "json")]
        public string? r_details_json { get; set; }

        /// <summary>
        /// Date et heure de l'événement (UTC)
        /// </summary>
        [Required]
        public DateTime r_created_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date d'expiration du token (pour déconnexions automatiques)
        /// </summary>
        public DateTime? r_token_expires_at { get; set; }

        /// <summary>
        /// Relation vers l'utilisateur
        /// </summary>
        [ForeignKey(nameof(r_user_id))]
        public virtual t_user? r_user { get; set; }
    }
}
