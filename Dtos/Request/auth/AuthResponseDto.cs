namespace ask.Dtos.Request.auth
{

    public class AuthResponseDto
    {
        public string type { get; set; }
        public string token { get; set; }
        public string refresh_token { get; set; }
        public int duree_token { get; set; }
        public int duree_refresh { get; set; }
        public Userdata userdata { get; set; }
        public Accesdata accesdata { get; set; }
    }

    public class Userdata
    {
        public int r_id { get; set; }
        public string r_username { get; set; }
        public string r_nomcomplet { get; set; }
    }

    public class Accesdata
    {
        public List<int> roleId { get; set; }
        public List<int> adminRoleId { get; set; }
        public int scopeListId { get; set; }
    }

}



