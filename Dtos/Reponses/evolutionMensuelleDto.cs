namespace ask.Dtos.Reponses
{
    public class evolutionMensuelleDto
    {
        public int recherches { get; set; }
        public int taches { get; set; }
        public int demandes { get; set; }
        public List<evolutionMoisDto> mois { get; set; } = new();
    }

    public class evolutionMoisDto
    {
        public int numero { get; set; }
        public string nom { get; set; } = string.Empty;
        public int taches { get; set; }
        public int recherches { get; set; }
        public int demandesAnnulation { get; set; }
    }
}
