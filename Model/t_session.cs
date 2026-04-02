using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    public class t_session : t_base
    {
        public string? r_token_jti { get; set; }
        public string? r_ip_address { get; set; }
        public string? r_user_agent { get; set; }
        public DateTime? r_login_at { get; set; }
        public DateTime? r_logout_at { get; set; }
        public bool r_is_active { get; set; } = true;

        [ForeignKey("t_user")]
        public int r_user_id_fk { get; set; }
        public t_user? r_userTab { get; set; }
    }
}
