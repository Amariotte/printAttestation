using print_attestation.ContextDb;
using print_attestation.Interface;
using print_attestation.Model;
using Microsoft.EntityFrameworkCore;

namespace print_attestation.Implementation
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
