namespace InteroperabiliteProject.RequestToReceiveDto
{
    public class ReponseNotificationEchec
    {
        public string? msgId { get; set; }
        public string? codeRaisonRejet { get; set; }
        public DateTime dateHeureRejet { get; set; }
        public string? emplacementErreur { get; set; }
        public string? descriptionRaisonRejet { get; set; }
        public string? infoAdditionnelle { get; set; }
    }
}



