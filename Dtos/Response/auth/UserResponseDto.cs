namespace ask.Dtos.Response.auth
{
    /// <summary>
    /// DTO de réponse pour la réinitialisation du mot de passe
    /// </summary>
    public class UserResponseDto
    {

        public int id { get; set; }
        public string nom { get; set; }
        public string prenom { get; set; }
        public string email { get; set; }
        public string telephone { get; set; }
        public string role { get; set; }
        public TYPE_USER roleId { get; set; }
        public Boolean actif { get; set; }
     
}
}
