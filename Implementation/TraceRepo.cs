
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;

namespace InteroperabiliteProject.Implementation
{
    public class TraceRepo : ITraceRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_trace> _dbset;
        public TraceRepo(InteropContext context) 
        {
            _context = context;
            _dbset = context.Set<t_trace>();
        }

        public async Task<(bool, string)> AddTrace(SENS_REQUETE_TRACE sensRequete, string _desc_route, string contenu_rq)
        {
            try
            {

                t_trace _tr = new t_trace
                {
                    dateEnvoie = DateTime.Now,
                    sensRequete = sensRequete,
                    titre = _desc_route,
                    Requete = contenu_rq
                };

                await _dbset.AddAsync(_tr);
                await _context.SaveChangesAsync();

                return (true, _tr.idrequette.ToString());
            }
            catch (Exception ex)
            {

               return (false, ex.Message);
            }
        }

        public async Task<(bool, string)> UpdateResTrace(int IdReq, string contenu_resp, string cleUnif = null)
        
        {
                try
                {

                    t_trace LaTrace = await _dbset.Where(p => p.idrequette == IdReq).FirstOrDefaultAsync();
                   
                    if (LaTrace == null)
                    {
                        return (false, "Ligne de trace non trouvée dans la bd");
                    }

                    LaTrace.ResponseRequete = contenu_resp;
                    LaTrace.datereponse = DateTime.Now;
                    LaTrace.cleUnifReqRep = cleUnif;

                    _dbset.Update(LaTrace);
                    await _context.SaveChangesAsync();

                    return (true, "");
                }
                catch (Exception ex)
                {

                    return (false, ex.Message);
                }
        }
    }
}