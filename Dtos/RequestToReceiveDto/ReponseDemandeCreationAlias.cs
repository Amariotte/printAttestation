namespace ask.Dtos.RequestToReceiveDto
{
    public class ReponseDemandeCreationAlias
    {
        public string idCreationAlias { get; set; }
        public string alias { get; set; }
        public string shid { get; set; }
        public string type { get; set; }
        public string statut { get; set; }
        public DateTime dateCreation { get; set; }
        public string raisonRejet { get; set; }
        public string informationsAdditionnelles { get; set; }
    }
}
