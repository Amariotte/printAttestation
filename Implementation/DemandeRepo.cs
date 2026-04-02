
using ask.ContextDb;
using ask.Interface;
using ask.Model;
using Microsoft.EntityFrameworkCore;

namespace ask.Implementation
{
    public class DemandeRepo : BaseRepo<t_demande>, IDemandeRepo
    {
        private readonly askContext _context;
        private readonly DbSet<t_demande> _dbset;
        public DemandeRepo(askContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_demande>();
        }

     

    }
}