namespace print_attestation.Dtos.Response
{
    public class evolutionDto
    {
        public int recherches { get; set; }
        public int taches { get; set; }
        public int demandes { get; set; }
        public string periode { get; set; } = "mois";
        public int? annee { get; set; }
        public List<evolutionPeriodeDto> periodes { get; set; } = new();
    }

    public class evolutionPeriodeDto
    {
        public int numero { get; set; }
        public string nom { get; set; } = string.Empty;
        public int recherches { get; set; }
        public tachesMoisDto? taches { get; set; }
        public demandesMoisDto? demandes { get; set; }
    }

   

    public class tachesMoisDto
    {
        public int total { get; set; }
        public int enCours { get; set; }
        public int annulees { get; set; }
        public int terminees { get; set; }
    }

    public class demandesMoisDto
    {
        public int attentes { get; set; }
        public int traitees { get; set; }
        public int rejetees { get; set; }
        public int total { get; set; }
    }
}
