using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface IclientRepo : IbaseRepo<t_client>
    {
        public Task<t_client> SearchClientByCodeClient(string codeClient);

    }


}
