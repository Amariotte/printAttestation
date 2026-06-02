using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    /// <summary>
    /// Portée (scope) pour les permissions utilisateur
    /// </summary>
    [Index(nameof(r_nom), IsUnique = true, Name = "IX_Scope_Nom")]
    public class t_scope : t_base
    {
        /// <summary>
        /// Nom unique du scope (ex: "users:read", "admin:write")
        /// </summary>
        [Required(ErrorMessage = "Le nom du scope est requis")]
        [MaxLength(100)]
        public string r_nom { get; set; } = string.Empty;

        /// <summary>
        /// Description du scope
        /// </summary>
        [MaxLength(500)]
        public string? r_description { get; set; }

        // Relations
        public ICollection<t_user_scopes>? r_user_scopes { get; set; }
    }
}
