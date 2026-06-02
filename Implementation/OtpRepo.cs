using ask.ContextDb;
using ask.Interface;
using ask.Model;
using Microsoft.EntityFrameworkCore;


namespace ask.Implementation
{
    public class OtpRepo : BaseRepo<t_otp>, IotpRepo
    {
        private readonly askContext _context;
        private readonly DbSet<t_otp> _dbset;
        public OtpRepo(askContext context) : base(context)
        {
            _context = context;
            _dbset = context.Set<t_otp>();
        }

        public async Task<t_otp> genererOtp(int userID,string IdOperationParent, TYPE_OTP type, int duree_validite)
        {

            try
            {


                // Désactiver tous les autres otp actif de l'opération
                var otpActifs = await _context.t_otp
                    .Where(o => o.r_is_active == true && o.r_is_delete != true && o.r_operation_parent_id == IdOperationParent && o.r_user_id_fk == userID)
                    .ToListAsync();

                if (otpActifs.Count > 0)
                {
                    foreach (var o in otpActifs)
                    {
                        o.r_is_delete = false;
                    }


                }


                string otp = Tools.Tools.Generatechiffrealeatoire(6);

                t_otp new_otp = new t_otp
                {
                    r_code_otp = otp,
                    r_operation_parent_id = IdOperationParent,
                    r_user_id_fk = userID,
                    r_challenge_id = Guid.NewGuid().ToString("N"),
                    r_type = type,
                    r_duree_validite = duree_validite,
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

        public async Task<int> verifieOtp(int userID, string code_otp, TYPE_OTP type, string IdOperationParent)
        {

            try
            {

                t_otp o = _context.t_otp
                    .Where(o => (o.r_code_otp == code_otp && o.r_is_delete != true && o.r_operation_parent_id == IdOperationParent && o.r_type == type && o.r_user_id_fk == userID))
                    .FirstOrDefault();


                if (o == null)
                    return -1; // OTP NOK

                if (o.r_is_active == false)
                    return -1; // OTP NOK

                TimeSpan DureeDeValidation = TimeSpan.FromMinutes(o.r_duree_validite);

                if (DateTime.Now - o.r_created_at <= DureeDeValidation)
                {
                    o.r_is_active = false;
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


        public async Task<(int,t_otp)> verifieOtpAndChallenge(string code_otp, TYPE_OTP type, string ChallengeId)
        {

            try
            {

                t_otp o = _context.t_otp
                    .Where(o => (o.r_code_otp == code_otp && o.r_is_delete != true && o.r_challenge_id == ChallengeId && o.r_type == type))
                    .FirstOrDefault();


                if (o == null)
                    return (-1,null); // OTP NOK

                if (o.r_is_active == false)
                    return (-1,null); // OTP NOK

                TimeSpan DureeDeValidation = TimeSpan.FromMinutes(o.r_duree_validite);

                if (DateTime.Now - o.r_created_at <= DureeDeValidation)
                {
                    o.r_is_active = false;
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
