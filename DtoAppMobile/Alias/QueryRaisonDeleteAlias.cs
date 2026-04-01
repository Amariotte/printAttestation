namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryRaisonDeleteAlias
    {
        public string? alias { get; set; }
        public int raison { get; set; }
    }


    public enum RAISON_DELETE {
        DEMANDE_CLIENT = 1,
        FERMETURE_COMPTE_CLIENT = 2
    }
}