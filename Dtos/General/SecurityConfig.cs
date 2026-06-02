namespace ask.Dtos.General
{
    public class SecurityConfig
    {

        public RateLimiter RateLimiter { get; set; }


    }


    public class RateLimiter
    {

        public int tokenLimit { get; set; } = 10;
        public int tokensPerPeriod { get; set; } = 10;
        public int minutes { get; set; } = 1;
   


    }
}
