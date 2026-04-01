namespace ask.Model
{
    public class t_route_scope :t_base
    {
        
        public string r_libelle { get; set; }
        public string r_description { get; set; }
        public string r_controller { get; set; }
        public string r_action { get; set; }
        public string r_route { get; set; }
        public ICollection<t_scoped> r_scopedtab { get; set; }
    }
}
