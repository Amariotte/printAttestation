namespace InteroperabiliteProject.DtoAppBusiness
{
    public class ScheduledListeDto
    {
        public string txId { get; set; }
        public string payeurAlias { get; set; }
        public string payeAlias { get; set; }
        public double montant { get; set; }
        public string motif { get; set; }
        public string statut { get; set; }
        public DateTime dateCreation { get; set; }
        public DateTime dateAcceptation { get; set; }
        public string rejectRaison { get; set; }



    }

}
