using FluentValidation;

namespace print_attestation.Dtos.Request
{
    public class siteDto
    {
        public string? nom { get; set; }
        public string? code { get; set; }
        public int? typeId { get; set; }
    }

    public class siteDtoValidator : AbstractValidator<siteDto>
    {
        public siteDtoValidator()
        {
            RuleFor(x => x.nom)
               .NotEmpty().WithMessage("Le nom est obligatoire.");

            RuleFor(x => x.code)
                .NotEmpty().WithMessage("Le code est obligatoire.");

          
            RuleFor(x => x.typeId)
                .GreaterThan(0).WithMessage("Le type doit être valide.");
        }
    }
}
