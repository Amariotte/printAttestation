
namespace InteroperabiliteProject.DtoAppMobile
{

    public class TransactionStatutDto
    {
        public string? statut { get; set; } // "en_attente" "irrevocable" "rejete"
        public string? codeRejet { get; set; } 
        public string? detailRejet { get; set; } 
        public DateTime? dateIrrevocabilite { get; set; } 
}


}
