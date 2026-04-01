using Newtonsoft.Json;

namespace InteroperabiliteProject.DtoAppMobile.Securite
{

    public class AuthResponseSecureDto
    {
        public bool result { get; set; }
        public string description { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public string type { get; set; }
        public string token { get; set; }
        public string refresh_token { get; set; }
        public int duree_token { get; set; }
        public int duree_refresh { get; set; }
        public bool is_pin_created { get; set; }
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
        public int r_id_app { get; set; }
        public List<int> roleId { get; set; }
        public List<int> adminRoleId { get; set; }
        public int scopeListId { get; set; }
    }

}



