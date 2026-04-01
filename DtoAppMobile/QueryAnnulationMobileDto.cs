using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile.Annulation
{
    public class QueryRejetMobileDto
    {
        public string? raison { get; set; }
    }

    public class QueryRejetMobileDtoValidator : AbstractValidator<QueryRejetMobileDto>
    {
        private static readonly HashSet<string> RaisonsValides = new HashSet<string>
    {
        "AM09", //Le montant reçu ne correspond pas au montant convenu ou attendu.
        "BE05", //La partie qui a initié le message n'est pas reconnue par le client final.
        "APAR", //Le paiement demandé a déjà été effectué par le payeur.
        "RR07", //Le justificatif de la demande de paiement est invalide (lorsque par exemple le numéro de facture est invalide)
         "FR01", //Suspicion de fraude
         "CUST", //Décision du client(envoyée pour rejeter une demande d'annulation)
          };


        public QueryRejetMobileDtoValidator()
        {
            RuleFor(x => x.raison)
                .NotEmpty().WithMessage("La raison est obligatoire.")
                .Must(r => RaisonsValides.Contains(r!))
                .WithMessage("La raison renseignée est incorrecte.");
        }
        }

  
}
