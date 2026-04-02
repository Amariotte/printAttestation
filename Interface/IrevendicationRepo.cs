using ask.Dtos.General;
using InteroperabiliteProject.Model;


namespace InteroperabiliteProject.Interface
{
    public interface IrevendicationRepo : IbaseRepo<t_revendication>
    {
      
        public Task<t_revendication> SearchRevendicationByIdAndSens(string id, sensFlux sens);
        public Task<t_revendication> SearchRevendicationById(string id);
        public Task<t_revendication> SearchRevendicationByCle(string cle);
        public Task<t_revendication> SearchRevendicationByIdPI(string idPI);


    }


}
