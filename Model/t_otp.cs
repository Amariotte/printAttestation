using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    public class t_otp :t_base
    {
        public string? codeOtp { get; set; }
        public string? challengeId { get; set; }
        public string? idOperationParent { get; set; }
        public TYPE_OTP type { get; set; } 
        public int dureeValidite { get; set; } = 5;// en minutes

        [ForeignKey("t_client")]
        public int r_client_id_fk { get; set; } = 0;


    }




}
