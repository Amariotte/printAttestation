using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace print_attestation.Model
{
    [Index(nameof(r_code), IsUnique = true, Name = "IX_Type_Site_Code")]
    [Index(nameof(r_libelle), Name = "IX_Type_Site_Libelle")]
    public class t_type_site : t_base
    {

        /// <summary>
        /// Nom de famille de l'utilisateur
        /// </summary>

        [Required(ErrorMessage = "Le code est requis")]
        [MaxLength(100)]
        public string r_code { get; set; } = string.Empty;


        [Required(ErrorMessage = "Le libellé est requis")]
        [MaxLength(100)]
        public string r_libelle { get; set; } = string.Empty;

        public ICollection<t_site>? r_sites { get; set; } = new List<t_site>();

    }
}
