namespace ask.Dtos.Request.auth
{
    public class AuthResponseSecureDto
    {
        public bool result { get; set; }
        public string? description { get; set; }
        public AuthResponseSecureData? data { get; set; }
    }

    public class AuthResponseSecureData
    {
        public string? token { get; set; }
        public string? refresh_token { get; set; }
        public string? type { get; set; }
        public int duree_token { get; set; }
        public int duree_refresh { get; set; }
        public bool is_pin_created { get; set; }
    }
}
