using InteroperabiliteProject.Dtos;
using static System.Net.WebRequestMethods;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryConfirmationCreationAlias
    {
        public string cle { get; set; }
        public string shid { get; set; }
        public string type  { get; set; }
        public string compte  { get; set; }
        public string pays { get; set; }
        public string otp { get; set; }
        public string client{ get; set; }
    }

}