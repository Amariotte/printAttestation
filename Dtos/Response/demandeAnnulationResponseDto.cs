
using System.ComponentModel.DataAnnotations;
using print_attestation.Dtos.Response.auth;

namespace print_attestation.Dtos.Response
{
    public class demandeAnnulationResponseDto
    {


        [MaxLength(200)]
     

        public int? id { get; set; }
     
        public int? userId { get; set; }
        public DateTime? dateTraitement { get; set; }
        public DateTime? createdAt { get; set; }
        public string? numPolice { get; set; } = null;
        public string? motifRejet { get; set; } = null;
        public string? numAttestation { get; set; } = null;
        public string? numImmatriculation { get; set; } = null;
        public STATUT_DEMANDE_ANNULATION? status { get; set; } = null;

        public string? motifLibelle { get; set; }
        public int? motifId { get; set; }
        public UserResponseDto? user { get; set; }
        public SiteResponseDto? site { get; set; }
        public List<demandeAnnulationFichierResponseDto>? fichiers { get; set; } = new List<demandeAnnulationFichierResponseDto>();


    }

 
}
