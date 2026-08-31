using print_attestation.ContextDb;
using print_attestation.Interface;
using print_attestation.Model;
using Microsoft.EntityFrameworkCore;


namespace print_attestation.Implementation
{
    public class UserRepo : BaseRepo<t_user>, IUserRepo
    {
        private readonly askContext _context;
        private readonly DbSet<t_user> _dbset;
        public UserRepo(askContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_user>();
        }

   

    }
}
