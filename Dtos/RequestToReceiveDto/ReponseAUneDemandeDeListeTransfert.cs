using InteroperabiliteProject.DtoAppMobile;

namespace ask.Dtos.RequestToReceiveDto
{
    public class ReponseAUneDemandeDeListeTransfert
    {
        public string? sens { get; set; }
        public string? nom { get; set; }
        public string? canal { get; set; }
        public double? montant { get; set; }
        public string? participant { get; set; }
        public string? dateheure { get; set; }
        public string? endToEndId { get; set; }
        public string? alias { get; set; }
        public string? iban { get; set; }
        public string? other { get; set; }
        public string? pays { get; set; }
        public string? motif { get; set; }     
        public string? typeTransaction { get; set; }     

    }



}
