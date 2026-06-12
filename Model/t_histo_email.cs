using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    /// <summary>
    /// Historique des emails envoyés
    /// </summary>
    [Index(nameof(r_recipients), Name = "IX_HistoEmail_Recipients")]
    [Index(nameof(r_statut), Name = "IX_HistoEmail_Statut")]
    [Index(nameof(r_created_at), Name = "IX_HistoEmail_CreatedAt")]
    public class t_histo_email : t_base
    {
        /// <summary>
        /// Adresse email de l'expéditeur
        /// </summary>
        [Required(ErrorMessage = "L'email de l'expéditeur est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [MaxLength(255)]
        public string r_sender_email { get; set; } = string.Empty;

        /// <summary>
        /// Nom de l'expéditeur
        /// </summary>
        [MaxLength(100)]
        public string? r_sender_name { get; set; }

        /// <summary>
        /// Corps de l'email (HTML ou texte)
        /// </summary>
        [Required(ErrorMessage = "Le corps de l'email est requis")]
        public string r_body { get; set; } = string.Empty;

        /// <summary>
        /// Sujet de l'email
        /// </summary>
        [Required(ErrorMessage = "Le sujet est requis")]
        [MaxLength(500)]
        public string r_subject { get; set; } = string.Empty;

        /// <summary>
        /// Destinataires (séparés par virgule si multiples)
        /// </summary>
        [Required(ErrorMessage = "Le(s) destinataire(s) est/sont requis")]
        [MaxLength(2000)]
        public string r_recipients { get; set; } = string.Empty;

        /// <summary>
        /// Statut de l'envoi
        /// </summary>
        public STATUT_EMAIL r_statut { get; set; } = STATUT_EMAIL.ATTENTE;

        /// <summary>
        /// Raison de l'échec (si applicable)
        /// </summary>
        [MaxLength(500)]
        public string? r_raison_echec { get; set; }

        /// <summary>
        /// Date d'envoi effectif
        /// </summary>
     //   public DateTime? r_sent_at { get; set; }

        /// <summary>
        /// Copie conforme (CC)
        /// </summary>
        [MaxLength(1000)]
        public string? r_cc { get; set; }

        /// <summary>
        /// Copie conforme invisible (BCC)
        /// </summary>
        [MaxLength(1000)]
        public string? r_bcc { get; set; }

        /// <summary>
        /// Indique si l'email est au format HTML
        /// </summary>
        public bool r_is_html { get; set; } = true;
    }
}
