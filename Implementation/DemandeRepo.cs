
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;

namespace InteroperabiliteProject.Implementation
{
    public class DemandeRepo : BaseRepo<t_demande>, IDemandeRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_demande> _dbset;
        public DemandeRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_demande>();
        }

        public async Task<(bool, string)> AddDemande(string controleur, string action, string contenu_rq, string titre, string description)
        {
            try
            {

                t_demande _newdemande = new t_demande
                {
                    dateheureRequete = DateTime.Now,
                    statut = Statut.EN_COURS,
                    r_request  = contenu_rq,
                    controleur = controleur,
                    action = action,
                     
                };

                await _dbset.AddAsync(_newdemande);
                await _context.SaveChangesAsync();

                return (true, _newdemande.Id.ToString());
            }
            catch (Exception ex)
            {

                return (false, ex.Message);
            }
        }

     

        public async Task<t_demande> RechercheByReference(string reference)
        {
            return await _dbset.Where(p => p.reference == reference).FirstOrDefaultAsync();
          
        }

        public async Task<t_demande> RechercheById(int id)
        {
            return await _dbset.Where(p => p.Id == id).FirstOrDefaultAsync();

        }

        public async Task<(bool, string)> UpdateDemandeById(int IdReq, string contenu_resp ,Statut statut)

        {
            try
            {

                t_demande LaDemande = await RechercheById(IdReq);

                if (LaDemande == null)
                {
                    return (false, "Ligne de demande non trouvée dans la bd");
                }

                LaDemande.r_response = contenu_resp;
                LaDemande.dateheureReponse = DateTime.Now;
                LaDemande.statut = statut;

                 _dbset.Update(LaDemande);
                await _context.SaveChangesAsync();

                return (true, "");
            }
            catch (Exception ex)
            {

                return (false, ex.Message);
            }
        }



        public async Task<(bool, string)> UpdateDemandeByReference(string reference, string contenu_resp, Statut statut)

        {
            try
            {

                t_demande LaDemande = await RechercheByReference(reference);

                if (LaDemande == null)
                {
                    return (false, "Ligne de demande non trouvée dans la bd");
                }

                LaDemande.r_response = contenu_resp;
                LaDemande.dateheureReponse = DateTime.Now;
                LaDemande.statut = statut;

                _dbset.Update(LaDemande);
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