using FluentValidation;

namespace ask.Dtos.Request.Auth
{
    public class ConnexionDto
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class ConnexionDtoValidator : AbstractValidator<ConnexionDto>
    {
        public ConnexionDtoValidator()
        {
            RuleFor(x => x.email)
                .NotEmpty().WithMessage("L'email est obligatoire.")
                .EmailAddress().WithMessage("L'adresse e-mail n'est pas valide.");

            RuleFor(x => x.password)
                .NotEmpty().WithMessage("Le mot de passe est obligatoire.");
        }
    }
}
