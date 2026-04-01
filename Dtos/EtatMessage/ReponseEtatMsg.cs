namespace InteroperabiliteProject.Dtos.EtatMessage
{
    public class ReponseEtatMsg
    {
        public DateTime dateEnvoi { get; set; }
        public string messageRecu { get; set; }
        public string msgId { get; set; }
        public string etat { get; set; }
        public DateTime dateReception { get; set; }
    }
}
