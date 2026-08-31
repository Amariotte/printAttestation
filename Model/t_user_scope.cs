
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace print_attestation.Model
{
 
    public class t_user_scope : t_base
    {
        [Required]
        [ForeignKey(nameof(r_scope))]
        public string? r_scope_code_fk { get; set; }

        public t_scope? r_scope { get; set; }


        [Required]
        [ForeignKey(nameof(r_user))]
        public int r_user_id_fk { get; set; }

        public t_user? r_user { get; set; }

    }
}
