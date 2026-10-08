using FluentValidation;

namespace print_attestation.Dtos.Request
{
    public class MotifAnnulationDto
    {
        public string? libelle { get; set; }
        public bool? besoinAtd { get; set; }
        public bool? besoinCpa { get; set; }
        public bool? besoinCarteGrise { get; set; }
        public bool? besoinOther { get; set; }
    }

    public class MotifAnnulationDtoValidator : AbstractValidator<MotifAnnulationDto>
    {
        public MotifAnnulationDtoValidator()
        {
            RuleFor(x => x.libelle)
               .NotEmpty().WithMessage("Le libelle est obligatoire.");
        }
    }
}
