namespace ask.Dtos.Request.auth
{


    public class RetourCreationUserDto
    {

        public bool result { get; set; }
        public string description { get; set; }
        public ClientData data { get; set; }

    }


    public class RetourSecureError
    {

        public bool result { get; set; }
        public string description { get; set; }
        public string[] error { get; set; }

    }

    public class ClientData
    {
        public int id { get; set; }
        public string? username { get; set; }
        public string? email { get; set; }
        public string nomcomplet { get; set; }
        public string telephone { get; set; }
        public string? nom { get; set; }
        public string? prenom { get; set; }
        public string? racine { get; set; }
    }






    public class UserSecurityData
    {
        public string id { get; set; }
        public string? username { get; set; }
        public string? email { get; set; }
        public string nomcomplet { get; set; }
        public string telephone { get; set; }
        public string? nom { get; set; }
        public string? prenom { get; set; }
        public string? racine { get; set; }
    }












}

