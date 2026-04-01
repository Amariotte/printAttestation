namespace ask.Dtos.RequestToReceiveDto
{
    public class ReponseDemandeListeParticipant
    {
        public string msgId { get; set; }
        public List<Participant> listeParticipant { get; set; }
    }
    public class Participant
    {
        public string codeMembreParticipant { get; set; }
        public string statut { get; set; }
        public string codeBanque { get; set; }
        public string nomOfficiel { get; set; }
    }

}

