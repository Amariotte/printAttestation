using System.Runtime.InteropServices;

namespace InteroperabiliteProject.Dtos.EnvoieController
{
    public class ReponseReceiveDemandeAnnulationDto
    {
        public string msgId { get; set; }
        public string codeMembreParticipantPaye { get; set; }
        public string statut { get; set; }
        public string endToEndId { get; set; }
        public string raison { get; set; }
    }



    public class ReponseSendDemandeAnnulationDto
{
    public string msgId { get; set; }
    public string msgIdDemande { get; set; }
    public string codeMembreParticipantPayeur { get; set; }
    public string statut { get; set; } = "RJCR";
    public string endToEndId { get; set; }
    public string raison { get; set; }
    }




    public class ReceiveDemandeAnnulationDto
    {
        public string msgId { get; set; }
        public string codeMembreParticipantPayeur { get; set; }
        public string endToEndId { get; set; }
        public string raison { get; set; }
    }


    public class SendDemandeAnnulationDto
    {
        public string msgId { get; set; }
        public string codeMembreParticipantPaye { get; set; }
        public string endToEndId { get; set; }
        public string raison { get; set; }
    }



}



