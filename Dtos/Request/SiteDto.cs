using FluentValidation;

namespace print_attestation.Dtos.Request
{
    public class SiteDto
    {
        public string? nom { get; set; }
        public string? code { get; set; }
    }

    public class SiteDtoValidator : AbstractValidator<SiteDto>
    {
        public SiteDtoValidator()
        {
            RuleFor(x => x.nom)
               .NotEmpty().WithMessage("Le nom est obligatoire.");

            RuleFor(x => x.code)
                .NotEmpty().WithMessage("Le code est obligatoire.");
        }
    }
}
