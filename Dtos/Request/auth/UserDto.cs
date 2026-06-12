using FluentValidation;

namespace ask.Dtos.Request.auth
{
    public class UserDto
    {
        public string? nom { get; set; }
        public string? prenom { get; set; }
        public string? email { get; set; }
        public string? telephone { get; set; }

        public TYPE_USER type { get; set; } = TYPE_USER.USER;
    }

    public class UserDtoValidator : AbstractValidator<UserDto>
    {
        public UserDtoValidator()
        {
            RuleFor(x => x.nom)
               .NotEmpty().WithMessage("Le nom est obligatoire.");

            RuleFor(x => x.prenom)
                .NotEmpty().WithMessage("Le prenom est obligatoire.");

            RuleFor(x => x.telephone)
                .NotEmpty().WithMessage("Le numéro de téléphone est obligatoire.")
                .Matches(@"^\d{8,15}$").WithMessage("Le numéro de téléphone doit contenir uniquement des chiffres (8 à 15 chiffres).");
            RuleFor(x => x.email)
                .EmailAddress().WithMessage("L'adresse e-mail n'est pas valide.")
                .When(x => !string.IsNullOrWhiteSpace(x.email));

        }
    }
}
