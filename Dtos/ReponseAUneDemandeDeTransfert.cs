namespace InteroperabiliteProject.Dtos
{
    public class ReponseAUneDemandeDeTransfert
    {
        public string msgId { get; set; }
        public string msgIdDemande { get; set; }
        public string endToEndId { get; set; }
        public string statutTransaction { get; set; }
        public string? codeRaison { get; set; }
        public string? informationsAdditionnelles { get; set; }
        public string? referenceBulk { get; set; }
        public string? identifiantTransaction { get; set; }
        public string? codeService { get; set; }


    }
}
