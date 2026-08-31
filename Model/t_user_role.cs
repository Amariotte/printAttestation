
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace print_attestation.Model
{
 
    public class t_user_role : t_base
    {
        [Required]
        [ForeignKey(nameof(r_role))]
        public int r_role_id_fk { get; set; }

        public t_role? r_role { get; set; }


        [Required]
        [ForeignKey(nameof(r_user))]
        public int r_user_id_fk { get; set; }

        public t_user? r_user { get; set; }

    }
}
