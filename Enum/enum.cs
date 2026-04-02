using System.Text.Json;


public enum STATUT_DEMANDE
{
    ATTENTE = 1,
    VALIDEE = 2,
    ANNULEE = 3,
    CLOTUREE = 4
}


public enum STATUT_EMAIL
{
    ATTENTE = 1,
    ENVOYE = 2,
    ECHOUE = 3
}

public enum STATUT_SMS
{
    ATTENTE = 1,
    ENVOYE = 2,
    ECHOUE = 3
}


public enum PLATEFORME_MESSAGERIE
{
    SMS = 1,
    EMAIL = 2
}

public enum TYPE_MODELE
{
    ENVOI_ACCESS = 1,
    MOT_PASSE_OUBLIE = 2,
}



public enum TYPE_OTP
{
    CONFIRMATION_REGISTER = 1,
    RESET_PASSWORD = 2
}