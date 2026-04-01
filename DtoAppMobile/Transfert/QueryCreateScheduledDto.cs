using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryCreateSouscriptionDto
    {
        public string? endToEndId { get; set; }
        public DateTime? dateDebut { get; set; }
        public DateTime? dateFin { get; set; }
        public string? frequence { get; set; } // "J", "S", "M", "A"
        public int? periodicite { get; set; }  // >= 2 (si fourni)
        public int? categorie { get; set; }
    }

    public class QueryCreateSouscriptionDtoValidator : AbstractValidator<QueryCreateSouscriptionDto>
    {
        private static readonly string[] frequenceValues = { "J", "S", "M", "A" };

        public QueryCreateSouscriptionDtoValidator()
        {
            RuleFor(data => data.endToEndId)
                .NotEmpty()
                .WithMessage("L'identifiant de la transaction est requis.");

            RuleFor(data => data.dateDebut)
                .NotNull()
                .WithMessage("La date de début est requise.")
                .Must(BeAFutureDate)
                .WithMessage("La date de début ne doit pas être antérieure à la date actuelle.");

            // Règle demandée : dateFin > dateDebut si renseignée
            RuleFor(data => data.dateFin)
                .GreaterThan(data => data.dateDebut!.Value)
                .When(data => data.dateFin.HasValue && data.dateDebut.HasValue)
                .WithMessage("La date de fin doit être strictement postérieure à la date de début.");

            RuleFor(data => data.frequence)
                .Must(f => string.IsNullOrEmpty(f) || frequenceValues.Contains(f))
                .WithMessage($"La fréquence doit être l'une des valeurs suivantes : {string.Join(", ", frequenceValues)}.");

            // (Optionnel) Si periodicite est fournie, elle doit être >= 2
            RuleFor(data => data.periodicite)
                .GreaterThanOrEqualTo(2)
                .When(data => data.periodicite.HasValue)
                .WithMessage("La périodicité doit être supérieure ou égale à 2.");
        }

        private bool BeAFutureDate(DateTime? date)
        {
            if (!date.HasValue) return false;
            return date.Value.Date > DateTime.UtcNow.Date;
        }
    }
}
