using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface IdemandeLigneRepo : IbaseRepo<t_demande_ligne>
    {
        public Task<t_demande_ligne> AddDemandeLigne(sensRequete sensReq, string iddemande, string contenu_rq, string uri, string description);

        public Task<bool> UpdateDemandeLigne(int idreq, Statut statut, int statusCode, string contenu_reponse);

       
        public Task<t_demande_ligne> SearchById(int idreq);


    }
}
