namespace ask.Dtos.RequestToReceiveDto
{
   

    public class RcpNotificationRegelementSoldeDto
    {
        public string msgId { get; set; }

        public soldes[] soldes { get; set; }
   
    }


    public class soldes
    {
        public string id { get; set; }

        public string dateDebutCompense { get; set; }
        public string dateFinCompense { get; set; }
        public string participant { get; set; }
        public string participantSponsor { get; set; }
        public string balanceType { get; set; } // "CLBD"
        public string montant { get; set; }
        public string operationType { get; set; } // "DBIT :  Le solde est débiteur" "CRDT : Le solde est créditeur"
        public string dateBalance { get; set; }
    }


}
