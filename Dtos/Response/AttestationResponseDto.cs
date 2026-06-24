using System;

namespace ask.Dtos.Response
{
    public class AttestationResponseDto
    {
        public string? numPolice { get; set; }
        public DateTime? dateEffet { get; set; }
        public DateTime? dateEcheance { get; set; }
        public string? marqueVehicule { get; set; }
        public string? typeVehicule { get; set; }
        public string? nomIntermediaire { get; set; }
        public string? numImmatriculation { get; set; }
        public string? numChassis { get; set; }
        public string? nomAssure { get; set; }
        public string? numAttestation { get; set; }
        public string? urlPdf { get; set; }
        public string? urlQr { get; set; }
        public string? urlImage { get; set; }
    }
}
