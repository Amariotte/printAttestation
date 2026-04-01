using FluentValidation;
using System.Linq;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryConfirmTransactionDto
    {
        public double montant { get; set; }
        public DateTime confirmationDate { get; set; }
        public string confirmationMethode { get; set; } // "biometry" "pin"
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public string? motif { get; set; }

    }


    public class QueryConfirmTransactionDtoValidator : AbstractValidator<QueryConfirmTransactionDto>
    {

        private static readonly List<string> methodConfirmes = new List<string> { "biometry", "pin" };
        public QueryConfirmTransactionDtoValidator()
        {
            RuleFor(data => data.confirmationDate)
              .NotEmpty()
              .WithMessage("La date de confirmation est requise.");

            RuleFor(data => data.confirmationMethode)
                .NotEmpty()
                .WithMessage("La méthode de confirmation est requise.")
                .Must(method => methodConfirmes.Contains(method))
                .WithMessage($"La méthode de confirmation doit être l'une des valeurs suivantes : {string.Join(", ", methodConfirmes)}.");

            RuleFor(data => data.montant)
                .NotNull()
                .WithMessage("Le montant est requis.")
                .GreaterThan(0)
                .WithMessage("Le montant doit être strictement supérieur à 0.");
           
        }


    }
}