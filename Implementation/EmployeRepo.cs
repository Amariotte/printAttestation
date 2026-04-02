using ask.ContextDb;
using ask.Interface;
using ask.Model;
using Microsoft.EntityFrameworkCore;

namespace ask.Implementation
{
    public class EmployeRepo : BaseRepo<t_employe>, IemployeRepo
    {

        protected readonly askContext _context;
        private readonly DbSet<t_employe> _dbset;
        public EmployeRepo(askContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_employe>();
        }

    }
}
