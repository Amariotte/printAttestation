using InteroperabiliteProject.Model;
using InteroperabiliteProject.ServicceAIP;

namespace InteroperabiliteProject.Interface
{
    public interface Iannulation_transfert : IbaseRepo<t_annulation_transfert>
    {
        public Task<t_annulation_transfert> CheckifReceptionExisteByEndToEnd(string endToEnd);
        public Task<bool> UpdateAllAnnulationsEnCours(string endToEnd, statutAnnulation statut);
    }
}
