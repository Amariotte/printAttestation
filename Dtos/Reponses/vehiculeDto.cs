namespace ask.Dtos.Reponses
{
    public class VehiculeDto
    {
        public string? numImatriculation { get; set; }
        public string numPolice { get; set; }
        public DateTime dateDebut { get; set; }
        public DateTime dateFin { get; set; }
        public string? marque { get; set; } = null;

        public string? numAttestation { get; set; }
 
    }
}
