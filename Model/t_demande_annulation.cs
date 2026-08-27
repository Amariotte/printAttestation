using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
{
    public class t_demande_annulation : t_base
    {
        [Required]
        [MaxLength(100)]

        /// <summary>
        /// Utilisateur qui a lancé la tâche (référence à t_user.r_id)
        /// </summary>

        public STATUT_DEMANDE_ANNULATION? r_status { get; set; }

        [MaxLength(200)]
        public string? r_num_police { get; set; }

        [MaxLength(200)]
        public string? r_num_attestation { get; set; }

        [MaxLength(200)]
        public string? r_num_immatriculation { get; set; }

        public string? r_motif_rejet { get; set; }

        public DateTime? r_date_traitement { get; set; }

        public int? r_site_id_fk { get; set; }

        public t_site? r_site { get; set; }

        [Required]
        [ForeignKey(nameof(r_user))]
        public int r_user_id_fk { get; set; }

        /// <summary>
        /// Relation de navigation vers l'utilisateur
        /// </summary>
        public t_user? r_user { get; set; }


        [Required]
        [ForeignKey(nameof(r_motif_annulation))]
        public int r_motif_annulation_id_fk { get; set; }

        /// <summary>
        /// Relation de navigation vers l'utilisateur
        /// </summary>
        public t_motif_annulation? r_motif_annulation { get; set; }

    }

}
