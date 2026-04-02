using ask.Model;

namespace ask.Interface
{
    public interface IotpRepo : IbaseRepo<t_otp>
    {
        public Task<int> verifieOtp(int userID, string code_otp, TYPE_OTP type, string IdOperationParent);

        public Task<(int, t_otp)> verifieOtpAndChallenge(string code_otp, TYPE_OTP type, string ChallengeId);

        public Task<t_otp> genererOtp(int userID, string IdOperationParent, TYPE_OTP type, int duree_validite);
    }
}
