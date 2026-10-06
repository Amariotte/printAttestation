using print_attestation.Dtos.Response.auth;

namespace print_attestation.Dtos.Request.auth
{
    public class AuthSecurityRetourDto
    {
      
        public UserResponseDto? user { get; set; }
        public sessionResponseDto? session { get; set; }
    }


    public class sessionResponseDto
    {
        public string? access_token { get; set; }
        public int expires_in { get; set; }
        public int refresh_expires_in { get; set; }
        public string? refresh_token { get; set; }
        public string? token_type { get; set; }

    }

}
