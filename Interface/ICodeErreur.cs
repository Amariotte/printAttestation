using InteroperabiliteProject.Model;
using InteroperabiliteProject.RequestToReceiveDto;

namespace InteroperabiliteProject.Interface
{
    public interface ICodeErreurRepo : IbaseRepo<t_code_erreur>
    {
        public Task<string> GetLibelleErreurAsync(string code, string? tag);




    }
}
