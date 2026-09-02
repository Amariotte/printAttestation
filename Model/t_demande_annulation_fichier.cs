using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace print_attestation.Model
{
    [Index(nameof(r_demande_annulation_id_fk), Name = "IX_DemandeAnnulationFichier_DemandeId")]
    public class t_demande_annulation_fichier : t_base
    {
        [Required]
        [ForeignKey(nameof(r_demande_annulation))]
        public int r_demande_annulation_id_fk { get; set; }

        [MaxLength(255)]
        public string? r_nom_fichier { get; set; }

        [MaxLength(2000)]
        public string? r_chemin_fichier { get; set; }

        public t_demande_annulation? r_demande_annulation { get; set; }
    }
}
