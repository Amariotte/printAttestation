using Microsoft.AspNetCore.Http;

namespace print_attestation.Dtos.Request
{
    public class demandeAnnulationCreateFormDto
    {
        public int motifAnnulationId { get; set; }
        public string? numPolice { get; set; }
        public string? numAttestation { get; set; }
        public string? numImmatriculation { get; set; }
        public List<IFormFile>? files { get; set; }
    }
}
