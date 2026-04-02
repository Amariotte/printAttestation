using ask.Services;

namespace ask.Dtos.RequestToSendDto
{
    public class SecurityConfig
    {

        public string pwd_client_default { get; set; }
        public string secure_method { get; set; }   //  secure ou Keyloack
        public int length_pin { get; set; }
        public secureConfig Secure { get; set; }
        public RateLimiter RateLimiter { get; set; }


    }


    public class RateLimiter
    {

        public int tokenLimit { get; set; } = 10;
        public int tokensPerPeriod { get; set; } = 10;
        public int minutes { get; set; } = 1;
   


    }
}
