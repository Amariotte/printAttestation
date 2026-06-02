using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    /// <summary>
    /// Table de jointure entre utilisateurs et scopes (permissions)
    /// </summary>
    public class t_user_scopes : t_base
    {
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
        /// Clé étrangère vers le scope
        /// </summary>
        [Required]
        [ForeignKey(nameof(r_scopeTab))]
        public int r_scope_id_fk { get; set; }

        /// <summary>
        /// Relation de navigation vers le scope
        /// </summary>
        public t_scope? r_scopeTab { get; set; }
    }
}
