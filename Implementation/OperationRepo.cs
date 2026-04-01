using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;


namespace InteroperabiliteProject.Implementation
{
    public class OperationRepo : BaseRepo<t_operation>, IoperationRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_operation> _dbset;
        public OperationRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_operation>();
        }

    }
}
