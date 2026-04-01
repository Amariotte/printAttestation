using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;

namespace InteroperabiliteProject.Implementation
{
    public class ClientRepo : BaseRepo<t_client>, IclientRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_client> _dbset;
        public ClientRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_client>();
        }


        public async Task<t_client> SearchClientByCodeClient(string? codeClient)
        {
            try
            {
                if (string.IsNullOrEmpty(codeClient)) 
                    return null;
               
                return _context.t_client.Where(o => o.code == codeClient && o.is_delete == false).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
