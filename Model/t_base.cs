using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ask.Model
{
    /// <summary>
    /// Classe de base pour tous les modèles avec audit et soft delete
    /// </summary>
    public abstract class t_base
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int r_id { get; set; }

        /// <summary>
        /// ID de l'utilisateur ayant créé l'enregistrement
        /// </summary>
        [JsonIgnore]
        public int? r_created_by { get; set; }

        /// <summary>
        /// Date et heure de création (UTC)
        /// </summary>
        [JsonIgnore]
        public DateTime? r_created_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date et heure de dernière modification (UTC)
        /// </summary>
        [JsonIgnore]
        public DateTime? r_updated_at { get; set; }

        /// <summary>
        /// ID de l'utilisateur ayant modifié l'enregistrement
        /// </summary>
        [JsonIgnore]
        public int? r_updated_by { get; set; }

        /// <summary>
        /// Indique si l'enregistrement est actif
        /// </summary>
        public bool r_is_active { get; set; } = true;

        /// <summary>
        /// Soft delete: indique si l'enregistrement est supprimé logiquement
        /// </summary>
        [JsonIgnore]
        public bool r_is_delete { get; set; } = false;
    }
}
