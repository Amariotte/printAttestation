namespace ask.Dtos.RequestToReceiveDto
{
    public class ReponseAUneDemandeDeVerificationIdentite
    {
        public string msgId { get; set; }
        public string msgIdDemande { get; set; }
        public string resultatVerification { get; set; }
        public string endToEndId { get; set; }
        public string ibanClient { get; set; }
        public string typeCompte { get; set; }
        public string nomClient { get; set; }
        public string villeClient { get; set; }
        public string adresseComplete { get; set; }
        public string numeroIdentification { get; set; }
        public string systemeIdentification { get; set; }
        public string dateNaissance { get; set; }
        public string villeNaissance { get; set; }
        public string paysNaissance { get; set; }
        public string paysResidence { get; set; }
        public string codeMembreParticipant { get; set; }
        public string numeroRCCMClient { get; set; }
        public string devise { get; set; }
        public string typeClient { get; set; }
        public string codeRaison { get; set; }
        public string otherClient { get; set; }
    }
}
