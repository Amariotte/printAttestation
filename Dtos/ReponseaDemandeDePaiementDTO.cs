namespace InteroperabiliteProject.Dtos
{
    public class ReponseaDemandeDePaiementDTO
    {
        public string msgId { get; set; }
        public string msgIdDemande { get; set; }
        public string identifiantDemandePaiement { get; set; }
        public string referenceBulk { get; set; }
        public string endToEndId { get; set; }
        public string statut { get; set; } = "RJCT";
        public string codeRaison { get; set; }
    }
}
