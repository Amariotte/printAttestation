namespace InteroperabiliteProject.Dtos.RevendicationAlias
{
    public class ReponseDemandeRevendicationDTO
    {
        public string alias { get; set; }
        public string identifiantRevendication { get; set; }
        public string statut { get; set; }
        public string detenteur { get; set; }
        public string revendicateur { get; set; }
        public string raisonRejet { get; set; }
        public string informationsAdditionnelles { get; set; }
        public DateTime dateCreation { get; set; }
        public DateTime dateModification { get; set; }
    }
}
