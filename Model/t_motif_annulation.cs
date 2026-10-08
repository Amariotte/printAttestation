using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace print_attestation.Model
{
    public class t_motif_annulation : t_base
    {

      
        [Required(ErrorMessage = "Le libellé est requis")]
        [MaxLength(100)]
        public string r_libelle{ get; set; } = string.Empty;

        public bool r_file_atd_required { get; set; } = false;

        public bool r_file_cpa_required { get; set; } = false;
        public bool r_file_other_required { get; set; } = false;
        public bool r_file_carte_grise_required { get; set; } = false;


        public ICollection<t_demande_annulation>? r_users { get; set; }
    }



}
