using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    public class t_role_scopes : t_base
    {
        [ForeignKey("t_role")]
        public int r_role_id_fk { get; set; }
        public t_role? r_roleTab { get; set; }

        [ForeignKey("r_scopeTab")]
        public int r_scope_id_fk { get; set; }
        public t_scope? r_scopeTab { get; set; }
    }
}
