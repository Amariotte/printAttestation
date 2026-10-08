using Microsoft.AspNetCore.Http;

namespace print_attestation.Dtos.Request
{
    public class demandeAnnulationCreateFormDto
    {
        public int motifId { get; set; }
        public string? siteCode { get; set; } = null;
        public int? siteId { get; set; }
        public string? numPolice { get; set; }
        public string? numAttestation { get; set; }
        public string? numImmatriculation { get; set; }
        public List<DemandeAnnulationFileDto>? files { get; set; }
    }


    public class DemandeAnnulationFileDto
    {
        public IFormFile file { get; set; } = null!;
        public TYPE_FICHIER typeId { get; set; }
    }
}
