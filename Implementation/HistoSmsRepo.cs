using print_attestation.ContextDb;
using print_attestation.Model;
using Microsoft.EntityFrameworkCore;
using print_attestation.Interface;

namespace print_attestation.Implementation
{
    public class HistoSmsRepo : BaseRepo<t_histo_sms>, IHistoSmsRepo
    {
        private readonly askContext _context;
        private readonly DbSet<t_histo_sms> _dbset;
        public HistoSmsRepo(askContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_histo_sms>();
        }


    }
}
