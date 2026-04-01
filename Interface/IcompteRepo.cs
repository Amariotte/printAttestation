using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface IcompteRepo : IbaseRepo<t_compte>
    {

        public Task<t_compte> SearchCompteByIbanOrOther(string numCpte, int idClient);



    }
}
