using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile.Annulation
{
    public class QueryAnnulationMobileDto
    {
        public string? raison { get; set; }
    }

    public class QueryAnnulationMobileDtoValidator : AbstractValidator<QueryAnnulationMobileDto>
    {
        private static readonly HashSet<string> RaisonsValides = new HashSet<string>
    {
        "AC03", // Erreur sur le destinataire
        "AM09", // Erreur sur le montant
        "SVNR", // Service non rendu
        "DUPL", // Transaction déjà payée
        "FRAD", // Suspicion de fraude
        "BE05", // Initiateur non reconnu
        "APAR", // Paiement déjà effectué
        "RR07", // Justificatif invalide
        "FR01", // Suspicion de fraude
        "CUST"  // Décision du client
    };

        public QueryAnnulationMobileDtoValidator()
        {
            RuleFor(x => x.raison)
                .NotEmpty().WithMessage("La raison est obligatoire.")
                .Must(r => RaisonsValides.Contains(r!))
                .WithMessage("La raison renseignée est incorrecte.");
        }
        }

  
}
