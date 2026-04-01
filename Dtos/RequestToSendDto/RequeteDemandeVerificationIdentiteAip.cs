namespace ask.Dtos.RequestToSendDto
{
    public class RequeteDemandeVerificationIdentiteAip
    {
        public string? msgId { get; set; }
        public string? msgIddemande { get; set; }
        public string? endToEndId { get; set; }
        public string? codeMembreParticipant { get; set; }
        public string? ibanClient { get; set; }
        public string? otherClient { get; set; }
    }


    public class RequeteDemandeVerificationIdentiteDto
    {
        public string codeMembreParticipant { get; set; }
        public string? ibanClient { get; set; }
        public string? otherClient { get; set; }
    }


    public class RequeteRechercheAliasDto
    {
        public string alias { get; set; }
        public bool demandePaiement { get; set; } = false;
   
    }
}
