using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    /// <summary>
    /// Historique des SMS envoyés
    /// </summary>
    [Index(nameof(r_recipient), Name = "IX_HistoSms_Recipient")]
    [Index(nameof(r_statut), Name = "IX_HistoSms_Statut")]
    [Index(nameof(r_created_at), Name = "IX_HistoSms_CreatedAt")]
    public class t_histo_sms : t_base
    {
        /// <summary>
        /// Expéditeur du SMS
        /// </summary>
        [Required(ErrorMessage = "L'expéditeur est requis")]
        [MaxLength(50)]
        public string r_sender { get; set; } = string.Empty;

        /// <summary>
        /// Contenu du SMS
        /// </summary>
        [Required(ErrorMessage = "Le texte est requis")]
        [MaxLength(1600)] // Limite SMS concatené
        public string r_text { get; set; } = string.Empty;

        /// <summary>
        /// Numéro du destinataire (format international)
        /// </summary>
        [Required(ErrorMessage = "Le destinataire est requis")]
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [MaxLength(20)]
        public string r_recipient { get; set; } = string.Empty;

        /// <summary>
        /// Statut de l'envoi
        /// </summary>
        public STATUT_SMS r_statut { get; set; } = STATUT_SMS.ATTENTE;

        /// <summary>
        /// Raison de l'échec (si applicable)
        /// </summary>
        [MaxLength(500)]
        public string? r_raison_echec { get; set; }

        /// <summary>
        /// Date d'envoi effectif
        /// </summary>
      //  public DateTime? r_sent_at { get; set; }

        /// <summary>
        /// ID externe du fournisseur SMS
        /// </summary>
        [MaxLength(100)]
        public string? r_provider_message_id { get; set; }
    }
}
