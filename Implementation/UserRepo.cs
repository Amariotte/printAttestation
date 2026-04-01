using ask.ContextDb;
using ask.Interface;
using ask.Model;
using Microsoft.EntityFrameworkCore;


namespace ask.Implementation
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
