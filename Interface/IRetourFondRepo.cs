using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;

namespace InteroperabiliteProject.Interface
{
    public interface IRetourFondRepo : IbaseRepo<t_retour_fonds>
    {

        public Task<bool> HasValidReturnByEndToEndIdAsync(string endToEndId);

        public Task<t_retour_fonds?> GetValidReturnByEndToEndIdAsync(string endToEndId);

    }
}
