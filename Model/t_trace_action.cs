using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    /// <summary>
    /// Modèle de traçabilité des actions utilisateur
    /// </summary>
    [Table("t_trace_action")]
    public class t_trace_action : t_base
    {
      

        public int? r_user_id { get; set; }

        /// <summary>
        /// Email de l'utilisateur (dénormalisé pour conservation historique)
        /// </summary>
        [MaxLength(255)]
        public string? r_user_email { get; set; }

        /// <summary>
        /// Type d'action effectuée (voir enum TYPE_ACTION)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string r_type_action { get; set; } = string.Empty;


        /// <summary>
        /// Détails de l'action au format JSON (ex: paramètres, résultats)
        /// </summary>
        [Column(TypeName = "json")]
        public string? r_details_json { get; set; }

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
        /// Méthode HTTP (GET, POST, PUT, DELETE, etc.)
        /// </summary>
        [MaxLength(10)]
        public string? r_http_method { get; set; }

        /// <summary>
        /// Chemin de l'endpoint appelé
        /// </summary>
        public string? r_endpoint { get; set; }
        public string? r_description { get; set; }
        /// <summary>
        /// Code de statut HTTP de la réponse (200, 404, 500, etc.)
        /// </summary>
        public int? r_status_code { get; set; }

        /// <summary>
        /// Durée de traitement en millisecondes
        /// </summary>
        public long? r_duration_ms { get; set; }

     
        /// <summary>
        /// Relation vers l'utilisateur
        /// </summary>
        [ForeignKey(nameof(r_user_id))]
        public virtual t_user? User { get; set; }
    }
}
