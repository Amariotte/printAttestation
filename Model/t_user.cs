using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    /// <summary>
    /// Modèle représentant un utilisateur du système
    /// </summary>
    [Index(nameof(r_email), IsUnique = true, Name = "IX_User_Email")]
    [Index(nameof(r_telephone), Name = "IX_User_Telephone")]
    public class t_user : t_base
    {
      
        /// <summary>
        /// Nom de famille de l'utilisateur
        /// </summary>
        [Required(ErrorMessage = "Le nom est requis")]
        [MaxLength(100)]
        public string r_nom { get; set; } = string.Empty;

      
        /// <summary>
        /// Numéro de téléphone (format international recommandé)
        /// </summary>
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [MaxLength(20)]
        public string? r_telephone { get; set; }

        /// <summary>
        /// Prénom de l'utilisateur
        /// </summary>
        [MaxLength(100)]
        public string? r_prenom { get; set; }

        /// <summary>
        /// Adresse email (unique, indexée)
        /// </summary>
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [MaxLength(255)]
        public string r_email { get; set; } = string.Empty;

        /// <summary>
        /// Hash du mot de passe (BCrypt)
        /// </summary>
        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MaxLength(255)]
        public string r_password { get; set; } = string.Empty;

    

        public bool r_password_change_required { get; set; } = true;
      

        /// <summary>
        /// Email vérifié
        /// </summary>
  
        /// </summary>
        public STATUT_USER r_statut { get; set; } = STATUT_USER.ACTIVE;
        public DateTime? r_date_last_statut { get; set; } 

        public TYPE_USER r_type { get; set; } = TYPE_USER.Utilisateur;

        // Relations de navigation
        public ICollection<t_refresh_token>? r_refresh_tokens { get; set; }
        public ICollection<t_session>? r_sessions { get; set; }
    }
}
