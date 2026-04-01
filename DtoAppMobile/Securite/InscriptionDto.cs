using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile.Securite
{
    public class InscriptionDto
    {
        public string? numerocompte { get; set; }
        public string? nom { get; set; }
        public string? email { get; set; }
        public string? telephone { get; set; }
        public string? password { get; set; }
    }

    public class InscriptionDtoValidator : AbstractValidator<InscriptionDto>
    {
        public InscriptionDtoValidator()
        {
            RuleFor(x => x.numerocompte)
                .NotEmpty().WithMessage("Le numéro de compte est obligatoire.");

            RuleFor(x => x.nom)
                .NotEmpty().WithMessage("Le nom est obligatoire.");

            RuleFor(x => x.telephone)
                .NotEmpty().WithMessage("Le numéro de téléphone est obligatoire.")
                .Matches(@"^\d{8,15}$").WithMessage("Le numéro de téléphone doit contenir uniquement des chiffres (8 à 15 chiffres).");
            RuleFor(x => x.email)
                .EmailAddress().WithMessage("L'adresse e-mail n'est pas valide.")
                .When(x => !string.IsNullOrWhiteSpace(x.email));

            RuleFor(x => x.password)
             .NotEmpty().WithMessage("Le mot de passe est obligatoire.");
        }
    }
}
