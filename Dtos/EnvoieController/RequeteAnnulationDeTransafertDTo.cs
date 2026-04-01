namespace InteroperabiliteProject.Dtos.EnvoieController
{
    public class RequeteSendAnnulationDeTransfertDTo
    {
        public string msgId { get; set; }
        public string codeMembreParticipantPaye { get; set; }
        public string endToEndId { get; set; }
        public string raison { get; set; }
    }

    public class RequeteReceiveAnnulationDeTransfertDTo
    {
        public string msgId { get; set; }
        public string codeMembreParticipantPayeur { get; set; }
        public string endToEndId { get; set; }
        public string raison { get; set; }
    }
}
