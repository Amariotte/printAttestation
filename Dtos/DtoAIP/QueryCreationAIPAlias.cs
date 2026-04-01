using FluentValidation;

namespace InteroperabiliteProject.DtoAIP
{
    public class QueryCreationAIPAlias
    {
        public int? idClient { get; set; }
        public string? cle { get; set; }
        public string? type { get; set; }
        public string? compte { get; set; }
        public string? plateforme { get; set; }
    }


    public enum PlateformeAPI
    {
        MOBILE = 1,
        BUSINESS = 2,
        BACKOFFICE = 3
    }

  
        public class QueryCreationAliasAIPValidator : AbstractValidator<QueryCreationAIPAlias>
        {
            private static readonly string[] AliasAutorises = { "SHID", "MBNO", "MCOD" };

            public QueryCreationAliasAIPValidator()
            {
                RuleFor(d => d.idClient)
                    .NotEmpty()
                    .WithMessage("L'identifiant du client est requis.");

                RuleFor(d => d.compte)
                    .NotEmpty()
                    .WithMessage("Le numéro de compte est requis.");

                // type: tolérer les espaces/casse puis vérifier la liste blanche
                RuleFor(d => d.type)
                    .NotEmpty().WithMessage("Le type d'alias est requis.")
                    .Must(t => AliasAutorises.Contains(t!, StringComparer.OrdinalIgnoreCase))
                    .WithMessage("Le type doit être 'SHID', 'MBNO' ou 'MCOD'.");

                // cle: obligatoire et format téléphone uniquement quand type = MBNO
                RuleFor(d => d.cle)
                    .NotEmpty().When(d => d.type != null && d.type.Equals("MBNO", StringComparison.OrdinalIgnoreCase))
                    .WithMessage("Le numéro de téléphone est requis.")
                    .Custom((cle, ctx) =>
                    {
                        var m = (QueryCreationAIPAlias)ctx.InstanceToValidate;
                        if (m.type != null && m.type.Equals("MBNO", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!string.IsNullOrWhiteSpace(m.cle))
                            {
                                if (!m.cle.StartsWith("+")) m.cle = "+" + m.cle;
                                if (!Tools.Tools.EstUnNumeroTelephone(m.cle))
                                    ctx.AddFailure("cle", "Le numéro de téléphone saisi n'est pas valide.");
                            }
                        }
                    });
            RuleFor(d => d.plateforme)
                .NotEmpty()
                .WithMessage("La plateforme est requise.")
                .Must(p => Enum.TryParse<PlateformeAPI>(p, true, out _))
                .WithMessage("La plateforme doit être 'MOBILE', 'BUSINESS' ou 'BACKOFFICE'.");
        }
        }

    }
