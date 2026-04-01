namespace InteroperabiliteProject.Dtos
{
    public class RetourEventDto
    {
        public type_notification? type { get; set; }
        public string? data { get; set; }       
    }
    

    public enum type_notification
    {

        NOTIFICATION_REPONSE_REQUETE,
        NOTIFICATION_REPONSE_RETOUR_FONDS,
        NOTIFICATION_ECHEC_FORMAT_ISO_INVALIDE,
        NOTIFICATION_REJET_ECHEC,
        NOTIFICATION_REPONSE_ECHEC,
        NOTIFICATION_ECHEC_500_504,
        NOTIFICATION_ECHEC_TRAITEMENT_AIP
    }
}