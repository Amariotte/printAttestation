namespace InteroperabiliteProject.DtoAppBusiness
{
  
    public class TransactionBusinessDto
    {
        public string endToEndId { get; set; }
        public double montant { get; set; }
        public string sens { get; set; }
        public string motif { get; set; }
        public string clientPSP { get; set; }
        public string? clientCompte { get; set; }
        public string dateIrrevocabilite { get; set; }
        public string clientNom { get; set; }
        public string clientPays { get; set; }
    }

}
