
namespace print_attestation.Dtos.Response
{
    public class demandeAnnulationFichierResponseDto
    {


        public int? id { get; set; }
        public string? nomFichier { get; set; } = null;
        public string? nomFichierSave { get; set; } = null;

        public TYPE_FICHIER? typeId { get; set; } = null;
        public string? typeLibelle { get; set; } = null;

    }



 
}
