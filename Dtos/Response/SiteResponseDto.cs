namespace print_attestation.Dtos.Response.auth
{
    /// <summary>
    /// DTO de réponse pour la réinitialisation du mot de passe
    /// </summary>
    public class siteResponseDto
    {

        public int id { get; set; }
        public string nom { get; set; }
        public string code { get; set; }
        public string typeSiteId { get; set; }

        public siteTypeResponseDto? typeSite { get; set; }


    }
}
