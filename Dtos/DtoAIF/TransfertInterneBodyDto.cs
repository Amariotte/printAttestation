namespace InteroperabiliteProject.Dtos
{

    public class TransfertInterneBodyDto
    {
        public string? identifiantTransaction { get; set; }
        public string? endToEndId { get; set; }
        public string? msgId { get; set; }
        public string? compteClientPayeur { get; set; }
        public string? nomClientPayeur { get; set; }
        public string? montant { get; set; }
        public string? compteClientPaye { get; set; }
        public string? nomClientPaye { get; set; }
        public string? motif { get; set; }

    }


}
