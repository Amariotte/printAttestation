using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    /// <summary>
    /// Modèle représentant un utilisateur du système
    /// </summary>
    [Index(nameof(r_email), IsUnique = true, Name = "IX_User_Email")]
    [Index(nameof(r_telephone), Name = "IX_User_Telephone")]
    [Index(nameof(r_code), Name = "IX_User_Code")]
    public class t_user : t_base
    {
        /// <summary>
        /// Code unique de l'utilisateur
        /// </summary>
        [MaxLength(50)]
        public string? r_code { get; set; }

        /// <summary>
        /// Nom de famille de l'utilisateur
        /// </summary>
        [Required(ErrorMessage = "Le nom est requis")]
        [MaxLength(100)]
        public string r_nom { get; set; } = string.Empty;

        /// <summary>
        /// URL ou chemin de la photo de profil
        /// </summary>
        [MaxLength(500)]
        public string? r_photo { get; set; }

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

        /// <summary>
        /// Date de dernière connexion (UTC)
        /// </summary>
        public DateTime? r_last_login_at { get; set; }

        /// <summary>
        /// Nombre de tentatives de connexion échouées
        /// </summary>
        public int r_failed_login_attempts { get; set; } = 0;

        /// <summary>
        /// Date de verrouillage du compte (si applicable)
        /// </summary>
        public DateTime? r_locked_until { get; set; }

        /// <summary>
        /// Email vérifié
        /// </summary>
        public bool r_email_verified { get; set; } = false;

        /// <summary>
        /// Téléphone vérifié
        /// </summary>
        public bool r_phone_verified { get; set; } = false;

        // Relations de navigation
        public ICollection<t_refresh_token>? r_refresh_tokens { get; set; }
        public ICollection<t_session>? r_sessions { get; set; }
        public ICollection<t_user_scopes>? r_user_scopes { get; set; }
    }
}
