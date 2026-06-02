using ask.ContextDb;
using ask.Interface;
using ask.Model;
using Microsoft.EntityFrameworkCore;

namespace ask.Implementation
{
    public class ModeleRepo : BaseRepo<t_modele>, ImodeleRepo
    {
        private readonly askContext _context;
        private readonly DbSet<ModeleRepo> _dbset;
        public ModeleRepo(askContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<ModeleRepo>();
        }
        public async Task<List<t_modele>> GetModelesByType( TYPE_MODELE type)
        {

            try
            {

                return await _context.t_modele
                    .Where(o => o.r_is_delete != true && o.r_type == type && o.r_is_active == true)
                    .ToListAsync();

            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
