using InteroperabiliteProject.Implementation;
using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface IscheduledRepo : IbaseRepo<t_scheduled>
    {

        public Task<t_scheduled> searchByTxId(string txId );


        public Task<t_scheduled> searchById(string Id);
    }
}
