using System.ComponentModel.DataAnnotations.Schema;

namespace InteroperabiliteProject.Model
{
    public class t_scoped:BaseClass
    {
        public string r_libelle { get; set; }
        public string r_description { get; set; }



        [ForeignKey(nameof(t_route_scope))]
        public int r_route_scope_fk { get; set; }
        public t_route_scope r_t_route_scope { get; set; }
    }
}
