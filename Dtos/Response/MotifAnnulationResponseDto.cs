namespace print_attestation.Dtos.Response
{
    public class MotifAnnulationResponseDto
    {
        public int? id { get; set; }
        public string? libelle { get; set; }
        public bool? besoinAtd { get; set; }
        public bool? besoinCpa { get; set; }
        public bool? besoinCarteGrise { get; set; }
        public bool? besoinOther { get; set; }
    }

   
}
