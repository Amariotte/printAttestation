using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    public class t_employe : t_base
    {
        public string r_nom { get; set; }
        public string? r_prenom { get; set; }
        public string? r_adresse { get; set; }
        public string? r_sexe { get; set; }
        public string? nationalite { get; set; }
        public string? r_telephone { get; set; }
        public string? r_date_naiss { get; set; }
        public string? r_ville_naiss { get; set; }


        [ForeignKey("t_direction")]
        public int? r_direction_FK { get; set; }
        public t_direction? r_directionTab { get; set; }
       }
}


