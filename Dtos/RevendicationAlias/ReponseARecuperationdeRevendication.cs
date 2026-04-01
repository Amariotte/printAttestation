namespace InteroperabiliteProject.Dtos.RevendicationAlias
{
    public class ReponseARecuperationdeRevendication: ReponseDemandeRevendicationDTO
    {
        public DateTime dateAction { get; set; }
        public string auteurAction { get; set; }
        public DateTime dateVerrouilage { get; set; }
    }
}
