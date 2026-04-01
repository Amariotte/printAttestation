using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace InteroperabiliteProject.Interface
{
    public interface ITraceRepo 
    {
        public Task<(bool,string)> AddTrace(SENS_REQUETE_TRACE sensRequete, string _desc_route, string contenu_rq);

        public Task<(bool, string)> UpdateResTrace(int IdReq, string contenu_resp, string cleUnif = null);
    }
}
