using FluentValidation;
using InteroperabiliteProject.Tools;

namespace InteroperabiliteProject.DtoAIP
{
    public class QueryBodyInitierPaiementOrTransfertAIFDto
    {
        public string payeurAlias { get; set; } // Alias du client connecté
        public double? montant { get; set; }
        public string? motif { get; set; }
        public string canal { get; set; } // "633" "731" "400" "631"
        public string? latitude { get; set; }
        public string? longitude { get; set; }
        public string? txId { get; set; } // L'identifiant de la transaction extraite du QR Code
        public bool? confirmation { get; set; } = true;// Attendre confirmation avant d'envoyer le transfert

        // Pour les validations de PI
        public string? identifiantMandat { get; set; }
        public string? signatureNumeriqueMandat { get; set; }
        public double? tauxRemisePaiementImmediat { get; set; }
        public bool? autorisationModificationMontant { get; set; }
        public double? montantRemisePaiementImmediat { get; set; }
        public string? typeDocumentReference { get; set; }
        public string? numeroDocumentReference { get; set; }
        public string? referenceBulk { get; set; }
        public string? typeTransaction { get; set; }
        public double? montantAchat { get; set; }
        public double? montantRetrait { get; set; }
        public double? fraisRetrait { get; set; }
        public DateTime? dateHeureExecution { get; set; }
        public DateTime? dateHeureLimiteAction { get; set; }

        public string? payeAlias { get; set; } // Alias du bénéficiaire
        public string? payeIban { get; set; }  // iban du bénéficiaire
        public string? payeOthr { get; set; }  // Autre référence de compte du bénéficiaire
        public string? payePSP { get; set; }   // Code PSP du bénéficiaire
    }

    public class QueryBodyInitierPaiementOrTransfertAIFDtoValidator : AbstractValidator<QueryBodyInitierPaiementOrTransfertAIFDto>
    {
        private static readonly string[] CanauxAutorises = { "731", "633", "999", "300", "000", "401", "733" };

        public QueryBodyInitierPaiementOrTransfertAIFDtoValidator()
        {
            // Canal
            RuleFor(data => data.canal)
               .NotEmpty()
               .WithMessage("Le canal est requis.")
               .Must(canal => CanauxAutorises.Contains(canal))
               .WithMessage($"Le canal doit être l'une des valeurs suivantes : {string.Join(", ", CanauxAutorises)}.");

            // Montant
            RuleFor(data => data.montant)
                .NotNull()
                .WithMessage("Le montant est requis.")
                .GreaterThan(0)
                .WithMessage("Le montant doit être strictement supérieur à 0.");

            // Alias payeur
            RuleFor(data => data.payeurAlias)
                .NotEmpty()
                .WithMessage("L'alias du payeur est requis.");

            // Coordonnées : obligatoires seulement si canal ≠ 300/999
            RuleFor(data => data.latitude)
                .NotEmpty()
                .WithMessage("La latitude de l'utilisateur est requise.")
                .When(d => d.canal != "300" && d.canal != "999");

            RuleFor(data => data.longitude)
                .NotEmpty()
                .WithMessage("La longitude de l'utilisateur est requise.")
                .When(d => d.canal != "300" && d.canal != "999");

            // TxId requis si le canal l'exige
            RuleFor(data => data.txId)
                .NotEmpty()
                .WithMessage("Le TxId est requis pour ce canal.")
                .When(d => Tools.Tools.canal_BesoinIDTrans(d.canal));

            // Validation des infos bénéficiaire
            RuleFor(data => data)
             .Custom((dto, context) =>
             {
                 // Si alias, iban et othr sont tous vides → erreur générale
                 if (string.IsNullOrWhiteSpace(dto.payeAlias) &&
                     string.IsNullOrWhiteSpace(dto.payeOthr) &&
                     string.IsNullOrWhiteSpace(dto.payeIban))
                 {
                     context.AddFailure("payeAlias / payeOthr / payeIban", "Les informations concernant le bénéficiaire sont invalides.");
                 }

                 // Si alias est vide, othr ou iban + payePSP sont requis
                 if (string.IsNullOrWhiteSpace(dto.payeAlias))
                 {
                     if (string.IsNullOrWhiteSpace(dto.payeOthr) && string.IsNullOrWhiteSpace(dto.payeIban))
                     {
                         context.AddFailure(nameof(dto.payeOthr), "La référence du compte (payeOthr) ou (payeIban) du bénéficiaire est requise si l’alias n’est pas fourni.");
                         context.AddFailure(nameof(dto.payeIban), "La référence du compte (payeOthr) ou (payeIban) du bénéficiaire est requise si l’alias n’est pas fourni.");
                     }

                     if (string.IsNullOrWhiteSpace(dto.payePSP))
                     {
                         context.AddFailure(nameof(dto.payePSP), "Le code PSP du bénéficiaire est requis si l’alias n’est pas fourni.");
                     }
                 }
             });
        }
    }
}
