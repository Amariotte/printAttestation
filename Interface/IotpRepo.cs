using InteroperabiliteProject.Model;

namespace InteroperabiliteProject.Interface
{
    public interface IotpRepo : IbaseRepo<t_otp>
    {
        public Task<int> verifieOtp(int clientID,string code_otp, type_otp type, string IdOperationParent);

        public Task<(int, t_otp)> verifieOtpAndChallenge(string code_otp, type_otp type, string ChallengeId);

        public Task<t_otp> genererOtp(int clientID, string IdOperationParent, type_otp type, int duree_validite);
    }
}
