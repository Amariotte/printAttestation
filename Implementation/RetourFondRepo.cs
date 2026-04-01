using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;

namespace InteroperabiliteProject.Implementation
{
    public class RetourFondRepo : BaseRepo<t_retour_fonds>, IRetourFondRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_retour_fonds> _dbset;
        public RetourFondRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_retour_fonds>();
        }


        public Task<bool> HasValidReturnByEndToEndIdAsync(string endToEndId)
                => _context.t_retour_fonds
                       .AsNoTracking()
                       .AnyAsync(rf => rf.endToEndId == endToEndId &&
                                       (rf.statut == statutRetourFond.irrevocable || rf.etape == etapeRetourFond.valide));

        public Task<t_retour_fonds?> GetValidReturnByEndToEndIdAsync(string endToEndId)
            => _context.t_retour_fonds
                   .AsNoTracking()
                   .Where(rf => rf.endToEndId == endToEndId &&
                                (rf.statut == statutRetourFond.irrevocable || rf.etape == etapeRetourFond.valide))
                   .OrderByDescending(rf => rf.dateHeureIrrevocabilite) // le plus récent d’abord
                   .FirstOrDefaultAsync();

    }

}
