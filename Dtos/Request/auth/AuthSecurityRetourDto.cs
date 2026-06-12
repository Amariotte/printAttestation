using ask.Dtos.Response.auth;

namespace ask.Dtos.Request.auth
{
    public class AuthSecurityRetourDto
    {
        public string? access_token { get; set; }
        public int expires_in { get; set; }
        public int refresh_expires_in { get; set; }
        public string? refresh_token { get; set; }
        public string? token_type { get; set; }
        public bool password_change_required { get; set; }

        public UserResponseDto? user { get; set; }
    }
}
