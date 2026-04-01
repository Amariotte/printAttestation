
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using InteroperabiliteProject.RequestToReceiveDto;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace InteroperabiliteProject.Implementation
{
    public class ParticipantsRepo : BaseRepo<t_participant>, IParticipantsRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_participant> _dbset;
        public ParticipantsRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_participant>();
        }

        public async Task<List<t_participant>> getAll()
        {
            return await _dbset.ToListAsync();
        }

        public async Task<t_participant> searchParticipant (string codeMembre)
        {

            var item = await _dbset.Where(p => p.codeMembreParticipant == codeMembre).FirstOrDefaultAsync();
            if (item == null) return null;
            return item;
        }



       
    }
}