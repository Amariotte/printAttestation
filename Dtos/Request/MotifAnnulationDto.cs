using FluentValidation;

namespace print_attestation.Dtos.Request
{
    public class MotifAnnulationDto
    {
        public string? libelle { get; set; }
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
