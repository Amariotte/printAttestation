using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    public class t_refresh_token : t_base
    {
        public string? r_token { get; set; }
        public string? r_jti { get; set; }
        public DateTime? r_expires_at { get; set; }
        public bool r_is_revoked { get; set; } = false;
        public string? r_replaced_by { get; set; }
        public string? r_ip_address { get; set; }
        public string? r_user_agent { get; set; }

        [ForeignKey("t_user")]
        public int r_user_id_fk { get; set; }
        public t_user? r_userTab { get; set; }
    }
}
