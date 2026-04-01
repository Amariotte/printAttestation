using InteroperabiliteProject.Dtos;
using static System.Net.WebRequestMethods;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryRpdDemandePaiement
    {
        public decision decision { get; set; }   
    }



    public enum decision
    {
        ACCEPTE = 1,
        PROGRAMME = 2,
        REJETE = 3
    }
}