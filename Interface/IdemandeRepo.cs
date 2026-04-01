using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface IDemandeRepo : IbaseRepo<t_demande>
    {
        public Task<(bool, string)> AddDemande(string controleur, string action, string contenu_rq,string titre, string description);

        public Task<(bool, string)> UpdateDemandeById(int IdReq, string contenu_resp, Statut statut);
        public Task<(bool, string)> UpdateDemandeByReference(string reference, string contenu_resp, Statut statut);

        public Task<t_demande> RechercheByReference(string reference);

        public Task<t_demande> RechercheById(int IdReq);
    }
}
