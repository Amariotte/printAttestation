using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface IreferenceRepo : IbaseRepo<t_reference>
    {
        public Task<(bool,string)> EquivalenceAIF(string type, string valuerBanque);
    }
}
