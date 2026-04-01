namespace InteroperabiliteProject.RequestToReceiveDto
{
    public class ReponseEchecDto
    {
        public string? reference { get; set; } // Pour vérification et transferts
        public string? codeRaisonRejet { get; set; }
        public DateTime dateHeureRejet { get; set; }
        public string? emplacementErreur { get; set; }
        public string? descriptionRaisonRejet { get; set; }
        public string? infoAdditionnelle { get; set; }
    }
}



