using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryCreationAlias
    {
        public string? cle { get; set; }
        public string? type { get; set; }
        public string? compte { get; set; }
    }

    public class QueryCreationAliasValidator : AbstractValidator<QueryCreationAlias>
    {
        private static readonly string[] AliasAutorises = { "SHID", "MBNO" };

        public QueryCreationAliasValidator()
        {
            RuleFor(data => data.compte)
                .NotEmpty()
                .WithMessage("Le numéro de compte est requis.");

            RuleFor(data => data.type)
                .Must(type => AliasAutorises.Contains(type))
                .WithMessage("Le type doit être 'SHID' ou 'MBNO'.");

            RuleFor(data => data.cle)
                .NotEmpty()
                .When(data => data.type == "MBNO")
                .WithMessage("Le numéro de téléphone est requis.")
                .Custom((cle, context) =>
                {
                    var model = context.InstanceToValidate;

                    if (model.type == "MBNO")
                    {
                        if (!string.IsNullOrWhiteSpace(model.cle))
                        {
                            if (!model.cle.StartsWith("+"))
                            {
                                model.cle = "+" + model.cle;
                            }

                            if (!Tools.Tools.EstUnNumeroTelephone(model.cle))
                            {
                                context.AddFailure("cle", "Le numéro de téléphone saisi n'est pas valide.");
                            }
                        }
                    }
                });
        }
    }




}
