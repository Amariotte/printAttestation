using ask.ContextDb;
using ask.Implementation;
using ask.Interface;
using ask.Model;
using Microsoft.EntityFrameworkCore;

namespace InteroperabiliteProject.Implementation
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
