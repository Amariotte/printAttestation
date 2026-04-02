using FluentValidation;

namespace ask.Dtos.Request.auth
{
    public class ClientSecureDto
    {
        public string username { get; set; } = string.Empty; // Identifiant utilisateur
        public string password { get; set; } = string.Empty; // Mot de passe
        public string racine { get; set; } = string.Empty;   // Racine
        public string email { get; set; } = string.Empty;    // Email
        public string telephone { get; set; } = string.Empty;    // telephone
        public string nomcomplet { get; set; } = string.Empty; // Nom complet
        public string nom { get; set; } = string.Empty;      // Nom
        public string prenom { get; set; } = string.Empty;   // Prénom
    }



    public class ClientSecureDtoValidator : AbstractValidator<ClientSecureDto>
    {
        public ClientSecureDtoValidator()
        {
            RuleFor(user => user.username).NotEmpty().WithMessage("Le username est obligatoire.");
            RuleFor(user => user.racine).NotEmpty().WithMessage("La racine du client est obligatoire.");
            //RuleFor(user => user.attributes.id_abonne).NotEmpty().WithMessage("L'id abonné mybank est obligatoire.");
            //RuleFor(user => user.attributes.password).NotEmpty().WithMessage("Le mot de passe est obligatoire.");
        }
    }


}
