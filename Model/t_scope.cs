using System.ComponentModel.DataAnnotations;

namespace print_attestation.Model
{
    /// <summary>
    /// Modèle représentant un utilisateur du système
    /// </summary>
    public class t_scope
    {

        [Key]
        public string? r_code { get; set; }

        public string? r_nom { get; set; } = string.Empty;

     
        public string? r_description{ get; set; }

     

        // Relations de navigation
        public ICollection<t_user_scope>? r_user_scopes { get; set; }
        public ICollection<t_role_scope>? r_role_scopes { get; set; }



    }
}
