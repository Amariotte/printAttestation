using FluentValidation;
using InteroperabiliteProject.Tools;

public class QueryRevendiquerUnAlias
{
    public string? alias { get; set; }
    public string? compte { get; set; }
}


public class QueryRevendiquerUnAliasValidator : AbstractValidator<QueryRevendiquerUnAlias>
{

  

    public QueryRevendiquerUnAliasValidator()
    {
        RuleFor(data => data.compte)
            .NotEmpty()
            .WithMessage("Le numéro de compte est requis.");

        RuleFor(data => data.alias)
            .NotEmpty()
            .WithMessage("L'alias est requis.");

        RuleFor(data => data)
            .Custom((model, context) =>
            {
              
                    if (!string.IsNullOrWhiteSpace(model.alias))
                    {
                        var alias = model.alias;

                        if (!alias.StartsWith("+"))
                        {
                            alias = "+" + alias;
                        }

                        // Remplacer ici par ton utilitaire réel de validation de téléphone
                        if (!Tools.EstUnNumeroTelephone(alias))
                        {
                            context.AddFailure("alias", "Le numéro de téléphone saisi n'est pas valide.");
                        }
                }
            });
    }
}
