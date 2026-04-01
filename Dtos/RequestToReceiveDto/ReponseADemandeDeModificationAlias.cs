namespace ask.Dtos.RequestToReceiveDto
{
    public class ReponseADemandeDeModificationAlias
    {
        public string alias { get; set; }
        public string statut { get; set; }
        public DateTime dateModification { get; set; }
        public string raisonRejet { get; set; }
        public string informationsAdditionnelles { get; set; }
    }
}
