using print_attestation.ContextDb;
using print_attestation.Model;
using Microsoft.EntityFrameworkCore;
using print_attestation.Interface;

namespace print_attestation.Implementation
{
    public class HistoEmailRepo : BaseRepo<t_histo_email>, IHistoEmailRepo
    {
        private readonly askContext _context;
        private readonly DbSet<t_histo_email> _dbset;
        public HistoEmailRepo(askContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_histo_email>();
        }


    }
}
