using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InteroperabiliteProject.Model
{
    public class t_trace
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int idrequette { get; set; }
        public SENS_REQUETE_TRACE sensRequete { get; set; } // 1-RVAIP (Requette vers AIP) ,2-RDAIP (Reponse de L'AIP) ,3- RVBCK(requette vers Back-end), 4-RDBCK(reponse du Backend)
        public string titre { get; set; }
        public string? Requete { get; set; }
        public string? ResponseRequete { get; set; }
        public string? AsyncResponse{ get; set; }
        public string? ResponseAsyncResponse { get; set; }

        public DateTime? dateEnvoie { get; set; }
        public DateTime? datereponse { get; set; }

        public string? cleUnifReqRep { get; set; }
    }

    public enum SENS_REQUETE_TRACE
    {
        ReqVersAIP = 1,
        RespAPI = 2,
        ReqMobileVersBack = 3,
        ReqbusinessVersBack = 4,
        RespDuBack = 5,
        ReqFromAIP=6
    }

}
