using InteroperabiliteProject.ContextDb;
using InteroperabiliteProject.Interface;
using InteroperabiliteProject.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using static System.Net.WebRequestMethods;

namespace InteroperabiliteProject.Implementation
{
    public class OtpRepo : BaseRepo<t_otp>, IotpRepo
    {
        private readonly InteropContext _context;
        private readonly DbSet<t_otp> _dbset;
        public OtpRepo(InteropContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_otp>();
        }

        public async Task<t_otp> genererOtp(int clientID,string IdOperationParent, type_otp type, int duree_validite)
        {

            try
            {


                // Désactiver tous les autres otp actif de l'opération
                var otpActifs = await _context.t_otp
                    .Where(o => o.r_isactive == true && o.is_delete != true && o.idOperationParent == IdOperationParent && o.r_client_id_fk == clientID)
                    .ToListAsync();

                if (otpActifs.Count > 0)
                {
                    foreach (var o in otpActifs)
                    {
                        o.r_isactive = false;
                    }


                }


                string otp = Tools.Tools.Generatechiffrealeatoire(6);

                t_otp new_otp = new t_otp
                {
                    codeOtp = otp,
                    idOperationParent = IdOperationParent,
                    r_client_id_fk = clientID,
                    challengeId = Guid.NewGuid().ToString("N"),
                    type = type,
                    dureeValidite = duree_validite,
                };

                await _context.t_otp.AddAsync(new_otp);
                await _context.SaveChangesAsync(); // Sauvegarder les modifications

                return new_otp;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> verifieOtp(int clientID,string code_otp, type_otp type, string IdOperationParent)
        {

            try
            {

                t_otp o = _context.t_otp
                    .Where(o => (o.codeOtp == code_otp && o.is_delete != true && o.idOperationParent == IdOperationParent && o.type == type && o.r_client_id_fk == clientID))
                    .FirstOrDefault();


                if (o == null)
                    return -1; // OTP NOK

                if (o.r_isactive == false)
                    return -1; // OTP NOK

                TimeSpan DureeDeValidation = TimeSpan.FromMinutes(o.dureeValidite);

                if (DateTime.Now - o.r_createdon <= DureeDeValidation)
                {
                    o.r_isactive = false;
                     _context.t_otp.Update(o); // Désactivé l'otp
                    return 1; // OTP OK
                }
                   
                else
                    return 0; // OTP EXPIRE
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<(int,t_otp)> verifieOtpAndChallenge(string code_otp, type_otp type, string ChallengeId)
        {

            try
            {

                t_otp o = _context.t_otp
                    .Where(o => (o.codeOtp == code_otp && o.is_delete != true && o.challengeId == ChallengeId && o.type == type))
                    .FirstOrDefault();


                if (o == null)
                    return (-1,null); // OTP NOK

                if (o.r_isactive == false)
                    return (-1,null); // OTP NOK

                TimeSpan DureeDeValidation = TimeSpan.FromMinutes(o.dureeValidite);

                if (DateTime.Now - o.r_createdon <= DureeDeValidation)
                {
                    o.r_isactive = false;
                    _context.t_otp.Update(o); // Désactivé l'otp
                    return (1, o); // OTP OK
                }

                else
                    return (0, null); // OTP EXPIRE
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
