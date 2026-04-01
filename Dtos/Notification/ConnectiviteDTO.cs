namespace InteroperabiliteProject.Dtos.Notification
{
    public class ConnectiviteDTO
    {
        public string msgId { get; set; }
        public string evenement { get; set; }
        public string evenementDescription { get; set; }
        public string evenementDate { get; set; } = DateTime.UtcNow.ToString();

    }

    public enum Evenement
    {
        PING,
        MAIN
    }


    public static class EvenementExtensions
    {
        public static string ToLabel(this Evenement statut)
        {
            return statut switch
            {
                Evenement.PING => "PING",
                Evenement.MAIN => "MAIN",
                _ => statut.ToString()
            };
        }
    }
}
