using InteroperabiliteProject.Interface;
using Microsoft.EntityFrameworkCore;

namespace ask.Implementation
{
    public class AliasRepo : BaseRepo<t_alias>, IemployeRepo
    {

        private readonly InteropContext _context;
        private readonly DbSet<t_alias> _dbset;
        public AliasRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_alias>();
        }

        public async Task<(bool, string)> DeleteAlias(string alias)
        {
            try
            {
                t_alias _rech_alias = await SearchAliasByAlias(alias);

                if (_rech_alias == null)
                    return (false, "L'alias n'existe pas dans le système");

                _rech_alias.is_delete = true;
                _rech_alias.dateSuppressionAlias = DateTime.Now;
                _context.t_alias.Update(_rech_alias);
                _context.SaveChanges();

                return (true, "Suppression d'alias effectué avec succes");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

      

        public async Task<t_alias> SearchAliasByAlias(string alias)
        {
            try
            {
                return _context.t_alias.Where(o => ((o.valeurAlias == alias || o.shid == alias) && o.is_delete != true && o.r_isactive != false)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<t_alias> SearchAliasByIban(string Iban)
        {
            try
            {
                return _context.t_alias.Where(o => (o.iban == Iban)  && o.is_delete != true && o.r_isactive != false).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<t_alias> SearchAliasByIdCreation(string IdCreation)
        {
            try
            {
                return _context.t_alias.Where(o => (o.idCreationAlias == IdCreation) && o.is_delete != true  && o.r_isactive != false).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<t_alias> SearchAliasByIdClient(int idClient)
        {

            try
            {
                return _context.t_alias.Where(o => o.r_client_id_fk == idClient && o.is_delete != true && o.r_isactive != false).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<t_alias> SearchAliasByIdClientAndAlias(int idClient, string alias )
        {
            try
            {
                return _context.t_alias.Where(o => o.r_client_id_fk == idClient && o.is_delete != true && o.r_isactive != false && (o.valeurAlias == alias || o.shid == alias)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
