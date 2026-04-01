using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;

namespace InteroperabiliteProject.Implementation
{
    public class CompteRepo : BaseRepo<t_compte>, IcompteRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_compte> _dbset;
        public CompteRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_compte>();
        }

        public async Task<t_compte> SearchCompteByIbanOrOther(string compte, int idClient)
        {
            try
            {
                if (string.IsNullOrEmpty(compte))
                    return null;

                return _context.t_compte.Where(o => ( o.ibanOrOther == compte ||  (o.codeAgence + o.numeroCompte) == compte) && o.is_delete != true && o.r_client_id == idClient).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
