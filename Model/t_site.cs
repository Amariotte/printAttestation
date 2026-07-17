using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ask.Model
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
        [MaxLength(100)]
        public string r_nom { get; set; } = string.Empty;

        public ICollection<t_user>? r_users { get; set; }
    }
}
