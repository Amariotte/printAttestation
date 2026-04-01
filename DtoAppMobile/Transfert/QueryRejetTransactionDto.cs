using FluentValidation;
using System.Linq;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryRejetTransactionDto
    {

        public string raison { get; set; } // "biometry" "pin"
   

    }


    public enum TransactionRejectReason
    {
        BE05, // La partie qui a initié le message n'est pas reconnue par le client final.
        AM09, // Le montant reçu ne correspond pas au montant convenu ou attendu.
        APAR, // Le paiement demandé a déjà été effectué par le payeur.
        RR07, // Le justificatif de la demande de paiement est invalide.
        FR01, // Suspicion de fraude.
        CUST  // Décision du client (envoyée pour rejeter une demande d'annulation).
    }


    public class QueryRejetTransactionDtoValidator : AbstractValidator<QueryRejetTransactionDto>
    {

              public QueryRejetTransactionDtoValidator()
            {
                RuleFor(data => data.raison)
                    .NotEmpty()
                    .WithMessage("La raison du rejet est requise.")
                    .Must(BeAValidTransactionRejectReason)
                    .WithMessage("La raison du rejet n'est pas valide. Valeurs valides : BE05, AM09, APAR, RR07, FR01, CUST.");
            }

            private bool BeAValidTransactionRejectReason(string raison)
            {
                // Vérifie si la chaîne correspond à une valeur valide dans l'enum
                return Enum.IsDefined(typeof(TransactionRejectReason), raison);
            }

    }
}