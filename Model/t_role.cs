
namespace print_attestation.Model
{
    /// <summary>
    /// Modèle représentant un utilisateur du système
    /// </summary>
    public class t_role : t_base
    {
      
      
        public int? r_ordre { get; set; } 
        public string? r_nom { get; set; } = string.Empty;

  
        public string? r_code{ get; set; }
        public string? r_description{ get; set; }
        public TYPE_SITE[] r_sites_types { get; set; } = Array.Empty<TYPE_SITE>();


        // Relations de navigation
        public ICollection<t_user_role>? r_user_roles { get; set; } = new List<t_user_role>();
        public ICollection<t_role_scope>? r_role_scopes { get; set; } = new List<t_role_scope>();

    }
}
