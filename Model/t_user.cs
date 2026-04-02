using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    public class t_user : t_base
    {

        public string? r_code { get; set; }
        public string? r_nom { get; set; }
        public string? r_photo { get; set; }
        public string? r_telephone { get; set; }
        public string? r_prenom { get; set; }
        public string? r_email { get; set; }
        public string? r_password { get; set; }


        public ICollection<t_user_roles>? r_user_roles { get; set; }
    }
}
