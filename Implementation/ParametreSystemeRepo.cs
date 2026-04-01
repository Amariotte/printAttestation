
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;


namespace InteroperabiliteProject.Implementation
{
    public class ParametreSystemeRepo : BaseRepo<t_parametre_systeme>, IParametreSystemeRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_parametre_systeme> _dbset;
        public ParametreSystemeRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_parametre_systeme>();
        }

        public async Task<string> GetValeur(string cle, string? tag)
        {

            cle = cle.Trim();
            var baseQuery = _dbset.AsNoTracking()
                .Where(p => p.cle == cle); // () indispensables

            t_parametre_systeme? t;

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var tagTrim = tag.Trim();
                t = await baseQuery.Where(p => p.tag == tagTrim).FirstOrDefaultAsync();
                t ??= await baseQuery.FirstOrDefaultAsync();
            }
            else
            {
                t = await baseQuery.FirstOrDefaultAsync();
            }

            return t?.valeur ?? null;
        }







    }
}