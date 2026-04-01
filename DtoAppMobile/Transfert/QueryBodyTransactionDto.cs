using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile
{


    public class QueryBodyTransactionDto
    {
        public double? montant { get; set; }
        public string? action { get; set; } // "send_now" "send_schedule" "receive_now"
        public string? motif { get; set; }
        public string canal { get; set; } // "633" "731" "400" "631"
        public string? compte { get; set; } // Numéro de compte du client connecté
        public string? latitude { get; set; } // La latitude à laquelle se trouve le client payeur pour initier le transfert
        public string? longitude { get; set; } // La longitude à laquelle se trouve le client payeur pour initier le transfert
        public string? txId { get; set; } // L'identifiant de la transaction extraite du QR Code

        public string? dateDebut { get; set; } // Date de la première exécution
        public string? dateFin { get; set; } // Date de la dernière exécution
        public string? frequence { get; set; } // Fréquence de paiement // "J" "S" "M" "A"

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

        public string? alias { get; set; } // Alias du bénéficiaire
        public string? iban { get; set; } // iban du bénéficiaire
        public string? othr { get; set; } // Autre référence de compte du bénéficiaire
        public string? payePSP { get; set; } // Code PSP du bénéficiaire

    }


    public enum Type_initie
    {
        alias = 1,
        iban = 2,
        other = 3
    }

    public class QueryBodyTransactionDtoValidator : AbstractValidator<QueryBodyTransactionDto>
    {

        private static readonly string[] ActionsAutorisees = { "send_now", "send_schedule", "receive_now" };
        private static readonly string[] CanauxAutorises = { "633", "731", "400", "631", "000","999" };
       
        public QueryBodyTransactionDtoValidator()
        {
            RuleFor(data => data.action)
                .NotEmpty()
                .WithMessage("L'action est requise.")
                .Must(action => ActionsAutorisees.Contains(action))
                .WithMessage($"L'action doit être l'une des valeurs suivantes : {string.Join(", ", ActionsAutorisees)}.");

            RuleFor(data => data.canal)
               .NotEmpty()
               .WithMessage("Le canal est requis.")
               .Must(canal => CanauxAutorises.Contains(canal))
               .WithMessage($"Le canal doit être l'une des valeurs suivantes : {string.Join(", ", CanauxAutorises)}.");


            RuleFor(data => data.montant)
                .NotNull()
                .WithMessage("Le montant est requis.")
                .GreaterThan(0)
                .WithMessage("Le montant doit être strictement supérieur à 0.");

            RuleFor(data => data.compte)
                .NotEmpty()
                .WithMessage("Le numéro de compte du client connecté est requis.");

            RuleFor(data => data.latitude)
                .NotEmpty()
                .WithMessage("La latitude de l'utilisateur est requise.");

            RuleFor(data => data.longitude)
                .NotEmpty()
                .WithMessage("La longitude de l'utilisateur est requise.");

       //     RuleFor(data => data.motif)
        //        .NotEmpty()
        //        .WithMessage("Le motif de la transaction est requis.");

            RuleFor(data => data)
             .Custom((dto, context) =>
             {


                 if (dto.action == "receive_now" && string.IsNullOrEmpty(dto.alias))
                 {
                     context.AddFailure(nameof(dto.alias), "L'alias est obligatoire lorsque l'action est 'receive_now'.");
                 }

                 else
                 { 
                     // Si alias, iban et othr sont tous vides → erreur générale sur les infos du bénéficiaire
                     if (string.IsNullOrEmpty(dto.alias) && string.IsNullOrEmpty(dto.othr) && string.IsNullOrEmpty(dto.iban))
                 {
                     context.AddFailure("alias / othr / iban", "Les informations concernant le bénéficiaire sont invalides.");
                 }

                 // Si alias est vide, alors othr ET payePSP doivent être présents
                 if (string.IsNullOrEmpty(dto.alias))
                 {
                     if (string.IsNullOrEmpty(dto.othr) && string.IsNullOrEmpty(dto.iban))
                     {
                         context.AddFailure(nameof(dto.othr), "La référence du compte (othr) ou (iban) du bénéficiaire est requise si l’alias n’est pas fourni.");
                         context.AddFailure(nameof(dto.iban), "La référence du compte (othr) ou (iban) du bénéficiaire est requise si l’alias n’est pas fourni.");
                     }

                     if (string.IsNullOrEmpty(dto.payePSP))
                     {
                         context.AddFailure(nameof(dto.payePSP), "Le code PSP du bénéficiaire est requis si l’alias n’est pas fourni.");
                     }
                     }
                 }


                 if (dto.action == "send_schedule" && string.IsNullOrEmpty(dto.dateDebut))
                 {
                     context.AddFailure(nameof(dto.dateDebut), "La date de début est obligatoire lorsque l'action est 'send_schedule'.");
                 }
             });
        }
    }






    public class QueryBodyTransactionDispoDto
    {
        public double? montant { get; set; }
        public string? action { get; set; } // "send_now" "send_schedule" "receive_now"
        public string? motif { get; set; }
        public string? compte { get; set; } // Numéro de compte du client connecté
        public string? latitude { get; set; } // La latitude à laquelle se trouve le client payeur pour initier le transfert
        public string? longitude { get; set; } // La longitude à laquelle se trouve le client payeur pour initier le transfert

        public string? alias { get; set; } // Alias du bénéficiaire
        public string? iban { get; set; } // iban du bénéficiaire
        public string? othr { get; set; } // Autre référence de compte du bénéficiaire
        public string? payePSP { get; set; } // Code PSP du bénéficiaire

    }



    public class QueryBodyTransactionDispoDtoValidator : AbstractValidator<QueryBodyTransactionDispoDto>
    {

        private static readonly string[] ActionsAutorisees = { "send_now", "send_schedule", "receive_now" };

        public QueryBodyTransactionDispoDtoValidator()
        {
            RuleFor(data => data.action)
                .NotEmpty()
                .WithMessage("L'action est requise.")
                .Must(action => ActionsAutorisees.Contains(action))
                .WithMessage($"L'action doit être l'une des valeurs suivantes : {string.Join(", ", ActionsAutorisees)}.");

         
            RuleFor(data => data.montant)
                .NotNull()
                .WithMessage("Le montant est requis.")
                .GreaterThan(0)
                .WithMessage("Le montant doit être strictement supérieur à 0.");

            RuleFor(data => data.compte)
                .NotEmpty()
                .WithMessage("Le numéro de compte du client connecté est requis.");

            RuleFor(data => data.latitude)
                .NotEmpty()
                .WithMessage("La latitude de l'utilisateur est requise.");

            RuleFor(data => data.longitude)
                .NotEmpty()
                .WithMessage("La longitude de l'utilisateur est requise.");

            //     RuleFor(data => data.motif)
            //        .NotEmpty()
            //        .WithMessage("Le motif de la transaction est requis.");

            RuleFor(data => data)
             .Custom((dto, context) =>
             {


                 if (dto.action == "receive_now" && string.IsNullOrEmpty(dto.alias))
                 {
                     context.AddFailure(nameof(dto.alias), "L'alias est obligatoire lorsque l'action est 'receive_now'.");
                 }

                 else
                 {
                     // Si alias, iban et othr sont tous vides → erreur générale sur les infos du bénéficiaire
                     if (string.IsNullOrEmpty(dto.alias) && string.IsNullOrEmpty(dto.othr) && string.IsNullOrEmpty(dto.iban))
                     {
                         context.AddFailure("alias / othr / iban", "Les informations concernant le bénéficiaire sont invalides.");
                     }

                     // Si alias est vide, alors othr ET payePSP doivent être présents
                     if (string.IsNullOrEmpty(dto.alias))
                     {
                         if (string.IsNullOrEmpty(dto.othr) && string.IsNullOrEmpty(dto.iban))
                         {
                             context.AddFailure(nameof(dto.othr), "La référence du compte (othr) ou (iban) du bénéficiaire est requise si l’alias n’est pas fourni.");
                             context.AddFailure(nameof(dto.iban), "La référence du compte (othr) ou (iban) du bénéficiaire est requise si l’alias n’est pas fourni.");
                         }

                         if (string.IsNullOrEmpty(dto.payePSP))
                         {
                             context.AddFailure(nameof(dto.payePSP), "Le code PSP du bénéficiaire est requis si l’alias n’est pas fourni.");
                         }
                     }
                 }
             });
        }
    }



}