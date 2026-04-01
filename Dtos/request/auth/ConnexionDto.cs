using FluentValidation;

namespace ask.Dtos.Request.Auth
{
    public class ConnexionDto
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class ConnexionDtoValidator : AbstractValidator<ConnexionDto>
    {
        public ConnexionDtoValidator()
        {
            RuleFor(x => x.username)
                .NotEmpty().WithMessage("L'username est obligatoire.");

            RuleFor(x => x.password)
                .NotEmpty().WithMessage("Le mot de passe est obligatoire.");
        }
    }
}
