using FluentValidation;

namespace print_attestation.Dtos.Request
{
    public class SiteDto
    {
        public string? nom { get; set; }
        public string? code { get; set; }
        public int? type { get; set; }
    }

    public class SiteDtoValidator : AbstractValidator<SiteDto>
    {
        public SiteDtoValidator()
        {
            RuleFor(x => x.nom)
               .NotEmpty().WithMessage("Le nom est obligatoire.");

            RuleFor(x => x.code)
                .NotEmpty().WithMessage("Le code est obligatoire.");

          
            RuleFor(x => x.type)
                .IsInEnum().WithMessage("Le type doit être valide.");
        }
    }
}
