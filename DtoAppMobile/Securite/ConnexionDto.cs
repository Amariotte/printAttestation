using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile.Securite
{
    public class ConnexionDto
    {
        public string identifiant { get; set; }
        public string password { get; set; }
    }

    public class ConnexionDtoValidator : AbstractValidator<ConnexionDto>
    {
        public ConnexionDtoValidator()
        {
            RuleFor(x => x.identifiant)
                .NotEmpty().WithMessage("L'identifiant est obligatoire.");

            RuleFor(x => x.password)
                .NotEmpty().WithMessage("Le mot de passe est obligatoire.");
        }
    }
}
