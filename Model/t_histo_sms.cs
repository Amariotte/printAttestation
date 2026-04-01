
namespace ask.Model
{
    public class t_histo_sms : t_base
    {

        public string? r_sender { get; set; }
        public string? r_text { get; set; }
        public string? r_recipient { get; set; }
        public STATUT_SMS r_statut { get; set; } = STATUT_SMS.ATTENTE;
        public string? r_raison_echec { get; set; }

    }


    
}
