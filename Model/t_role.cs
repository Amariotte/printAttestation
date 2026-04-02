namespace ask.Model
{
    public class t_role : t_base
    {
        public string r_nom { get; set; }
        public string r_code { get; set; }
        public string? r_description { get; set; }

        public ICollection<t_user_roles>? r_user_roles { get; set; }
        public ICollection<t_role_scopes>? r_role_scopes { get; set; }
    }
}
