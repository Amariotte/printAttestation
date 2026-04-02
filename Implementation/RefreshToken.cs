using ask.ContextDb;
using ask.Interface;
using ask.Model;
using Microsoft.EntityFrameworkCore;

namespace ask.Implementation
{
    public class RefreshTokenRepo : BaseRepo<t_refresh_token>, IRefreshTokenRepo
    {

        protected readonly askContext _context;
        private readonly DbSet<t_refresh_token> _dbset;
        public RefreshTokenRepo(askContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_refresh_token>();
        }

    }
}
