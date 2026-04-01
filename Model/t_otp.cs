using System.ComponentModel.DataAnnotations.Schema;

namespace InteroperabiliteProject.Model
{
    public class t_otp :BaseClass
    {
        public string? codeOtp { get; set; }
        public string? challengeId { get; set; }
        public string? idOperationParent { get; set; }
        public type_otp type { get; set; } 
        public int dureeValidite { get; set; } = 5;// en minutes

        [ForeignKey("t_client")]
        public int r_client_id_fk { get; set; } = 0;


    }

    public enum type_otp
    {
        CREATION_ALIAS = 1,
        CONFIRMATION_TRANSFERT = 2,
        CONFIRMATION_RTP = 3,
        CONFIRMATION_PAIEMENT = 4,
        SUPPRESSION_ALIAS = 5,
        REJET_REVENDICATION = 6,
        RESET_CODE_PIN = 7,
        RESET_PASSWORD = 8,
        CONFIRMATION_REGISTER = 9
    }



}
