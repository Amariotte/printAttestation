using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    /// <summary>
    /// OTP (One-Time Password) pour authentification à deux facteurs
    /// </summary>
    [Index(nameof(r_challenge_id), IsUnique = true, Name = "IX_OTP_ChallengeId")]
    [Index(nameof(r_code_otp), Name = "IX_OTP_Code")]
    [Index(nameof(r_type), Name = "IX_OTP_Type")]
    [Index(nameof(r_created_at), Name = "IX_OTP_CreatedAt")]
    public class t_otp : t_base
    {
        /// <summary>
        /// Code OTP (généralement 4-6 chiffres)
        /// </summary>
        [Required(ErrorMessage = "Le code OTP est requis")]
        [MaxLength(10)]
        public string r_code_otp { get; set; } = string.Empty;

        /// <summary>
        /// ID unique du challenge (pour vérification)
        /// </summary>
        [Required(ErrorMessage = "Le challenge ID est requis")]
        [MaxLength(100)]
        public string r_challenge_id { get; set; } = string.Empty;

        /// <summary>
        /// ID de l'opération parente (ex: user_id, demande_id)
        /// </summary>
        [MaxLength(50)]
        public string? r_operation_parent_id { get; set; }

        /// <summary>
        /// Type d'OTP (inscription, réinitialisation mot de passe, etc.)
        /// </summary>
        [Required(ErrorMessage = "Le type d'OTP est requis")]
        public TYPE_OTP r_type { get; set; }

        /// <summary>
        /// Durée de validité de l'OTP en minutes
        /// </summary>
        [Range(1, 60, ErrorMessage = "La durée de validité doit être entre 1 et 60 minutes")]
        public int r_duree_validite { get; set; } = 5;

        /// <summary>
        /// Date et heure d'expiration calculée (UTC)
        /// </summary>
        [NotMapped]
        public DateTime ExpiresAt => r_created_at.AddMinutes(r_duree_validite);

        /// <summary>
        /// Vérifie si l'OTP est expiré
        /// </summary>
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// Date de vérification de l'OTP
        /// </summary>
        public DateTime? r_verified_at { get; set; }

        /// <summary>
        /// Nombre de tentatives de vérification
        /// </summary>
        public int r_attempts { get; set; } = 0;

        /// <summary>
        /// Nombre maximum de tentatives autorisées
        /// </summary>
        public int r_max_attempts { get; set; } = 3;

        /// <summary>
        /// Vérifie si l'OTP est toujours valide
        /// </summary>
        [NotMapped]
        public bool IsValid => !IsExpired && r_attempts < r_max_attempts && !r_verified_at.HasValue && r_is_active && !r_is_delete;

        /// <summary>
        /// Clé étrangère vers l'utilisateur (si applicable)
        /// </summary>
        [ForeignKey(nameof(r_userTab))]
        public int? r_user_id_fk { get; set; }

        /// <summary>
        /// Relation de navigation vers l'utilisateur
        /// </summary>
        public t_user? r_userTab { get; set; }
    }
}
