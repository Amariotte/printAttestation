using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface IcreationAliasRepo : IbaseRepo<t_creation_alias>
    {
        public Task<t_creation_alias> SearchByIdClientAndTelCreation(int id,string telephone);

        public Task<bool> DeleteAllByIdClientAndTelCreation(int id, string telephone);
    }
}
