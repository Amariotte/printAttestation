using System.ComponentModel.DataAnnotations;

namespace print_attestation.Model
{
    public class t_motif_annulation : t_base
    {

      
        [Required(ErrorMessage = "Le libellé est requis")]
        [MaxLength(100)]
        public string r_libelle{ get; set; } = string.Empty;

        public ICollection<t_demande_annulation>? r_users { get; set; }
    }
}
