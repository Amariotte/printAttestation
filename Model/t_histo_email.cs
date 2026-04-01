namespace ask.Model
{
    public class t_histo_email : t_base
    {

        public string? r_sender_email { get; set; }
        public string? r_sender_name { get; set; }
        public string? r_body { get; set; }
        public string? r_subject { get; set; }
        public string? r_recipients { get; set; }
        public STATUT_EMAIL r_statut { get; set; } = STATUT_EMAIL.ATTENTE;
        public string? r_raison_echec { get; set; }

    }


  
}
