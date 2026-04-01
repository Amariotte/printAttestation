
using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InteroperabiliteProject.Implementation
{
    public class DemandeligneRepo : BaseRepo<t_demande_ligne>, IdemandeLigneRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_demande_ligne> _dbset;
        public DemandeligneRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_demande_ligne>();
        }

        public async Task<t_demande_ligne> AddDemandeLigne(sensRequete sensReq,string iddemande, string contenu_rq, string uri , string description)
        {
            try
            {

                //*****************************************************************************************************************************
                t_demande_ligne t_dL = new t_demande_ligne
                {
                    r_uri = uri,
                    r_description = description,
                    r_dateheure_req = DateTime.Now,
                    r_demande_FK = Convert.ToInt32(iddemande),
                    r_requete = contenu_rq,
                    r_sens_req= sensReq,
                    Status = Statut.EN_COURS
                };

                //*****************************************************************************************************************************

                await _dbset.AddAsync(t_dL);
                await _context.SaveChangesAsync();

                return t_dL;
            }
            catch (Exception ex)
            {

                return null;
            }
        }


        public async Task<t_demande_ligne> SearchById(int idreq)
        {
            try
            {
               return await _dbset.Where(p => p.Id == idreq).FirstOrDefaultAsync(); ;
            }
            catch (Exception ex)
            {

                return null;
            }
        }


        public async Task<bool> UpdateDemandeLigne(int idreq, Statut statut,int statusCode,string contenu_reponse)

        {
            try
            {


                t_demande_ligne t_demandeligne = await SearchById(idreq);

                if (t_demandeligne != null)
                {
                    t_demandeligne.r_updatedon = DateTime.Now;
                    t_demandeligne.r_dateheure_rep = DateTime.Now;
                    t_demandeligne.Status = statut;
                    t_demandeligne.StatusCode = statusCode;
                    t_demandeligne.r_reponse = contenu_reponse;
                    _dbset.Update(t_demandeligne);
                    await _context.SaveChangesAsync();

                    return true;

                }

                return false;

            }
            catch (Exception ex)
            {

                return false;
            }
        }
    }
}