
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
 
    public class t_role_scope : t_base
    {
        [Required]
        [ForeignKey(nameof(r_scope))]
        public int r_scope_id_fk { get; set; }

        public t_scope? r_scope { get; set; }


        [Required]
        [ForeignKey(nameof(r_role))]
        public int r_role_id_fk { get; set; }

        public t_role? r_role { get; set; }

    }
}
