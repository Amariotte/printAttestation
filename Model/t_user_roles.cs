using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    public class t_user_roles : t_base
    {
        [ForeignKey("t_user")]
        public int r_user_id_fk { get; set; }
        public t_user? r_userTab { get; set; }

        [ForeignKey("t_role")]
        public int r_role_id_fk { get; set; }
        public t_role? r_roleTab { get; set; }

        public bool r_is_admin { get; set; } = false;
    }
}
