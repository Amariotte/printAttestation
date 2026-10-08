using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace print_attestation.Model
{
    [Index(nameof(r_code), IsUnique = true, Name = "IX_Site_Code")]
    [Index(nameof(r_nom), Name = "IX_Site_Nom")]
    public class t_site : t_base
    {

        /// <summary>
        /// Nom de famille de l'utilisateur
        /// </summary>

        [Required(ErrorMessage = "Le code est requis")]
        [MaxLength(100)]
        public string r_code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis")]
        [MaxLength(500)]
        public string r_nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le type est requis")]


        public int? r_type_site_id_fk { get; set; }

        [ForeignKey(nameof(r_type_site_id_fk))]

        public t_type_site? r_type_site { get; set; }

    

        public ICollection<t_user>? r_users { get; set; } = new List<t_user>();

    }
}
