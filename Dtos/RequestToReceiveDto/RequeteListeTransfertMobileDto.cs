using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ask.Dtos.RequestToReceiveDto
{
    public class RequeteListeTransfertMobileDto
    {

        public string alias { get; set; }
        public Filtre? filtre { get; set; }

    }


    public class Filtre
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }

}
