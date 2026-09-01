namespace print_attestation.Dtos.Response.auth
{
    /// <summary>
    /// DTO de réponse pour la réinitialisation du mot de passe
    /// </summary>
    public class UserResponseDto
    {

        public int id { get; set; }
        public string? nom { get; set; }
        public string? prenom { get; set; }
        public string? email { get; set; }
        public string? telephone { get; set; }
        public bool? actif { get; set; }

        public int?[] sites { get; set; }
        public string?[] roles { get; set; }


    }
}
