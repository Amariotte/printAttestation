namespace ask.Model

{
    public class t_log :t_base
    {
        public string? r_code{ get; set; }
        public string? r_description{ get; set; }
        public string? r_request{ get; set; }
        public string? r_response{ get; set; }
        public string? controleur{ get; set; }
        public string? action{ get; set; }
        public STATUT_DEMANDE? statut { get; set; }

        public string? reference { get; set; }
        public DateTime? r_date_demande { get; set; }
        public DateTime? r_date_validation { get; set; }


    }


}
