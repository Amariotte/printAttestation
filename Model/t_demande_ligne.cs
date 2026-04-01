using System.ComponentModel.DataAnnotations.Schema;

namespace ask.Model
{
    public class t_demande_ligne :t_base
    {

        public string r_description { get; set; }
        public string r_uri { get; set; }
        public string r_requete { get; set; }
        public string? r_reponse { get; set; }
        public DateTime r_dateheure_req { get; set; }
        public DateTime? r_dateheure_rep { get; set; }

        public sensRequete r_sens_req { get; set; }
        public int? StatusCode { get; set; }
        public Statut? Status { get; set; }


        [ForeignKey("t_demande")]
        public int r_demande_FK { get; set; }
        public t_demande r_demandeTab { get; set; }
    }

    public enum sensRequete
    {
        Req_Vers_AIF = 1,
        Req_Vers_AIP = 2,
        Req_From_AIP = 3,
        Req_Vers_Keyloack = 4,
        Req_Vers_Secure = 5

    }

}
