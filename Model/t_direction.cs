namespace ask.Model
{
    public class t_direction : t_base
    {
        public string? r_code { get; set; }
        public string? r_nom { get; set; }

        public ICollection<t_employe>? r_employes { get; set; }
    }
}


