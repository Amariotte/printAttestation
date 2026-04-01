using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface ItransfertAutoriseRepo : IbaseRepo<t_transfert_autorise>
    {
        public Task<bool> AvoirAutorisations(string typePayeur, string typePaye,string canal);
        public Task<string> ListeCanauxAuto(string typePayeur, string typePaye);

    }


}
