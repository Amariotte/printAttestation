using FluentValidation;
using System.Text.RegularExpressions;

namespace InteroperabiliteProject.DtoAppMobile.Securite
{
    //public class QueryCodePIN : AbstractValidator<QueryCodePIN>
    public class QueryCodePIN
    {
        public string? pin { get; set; }

    }

    public class QueryCodePINValidator : AbstractValidator<QueryCodePIN>
    {
        public QueryCodePINValidator(int lengthPin)
        {
            RuleFor(x => x.pin)
           .Cascade(CascadeMode.Stop)
          .NotEmpty().WithMessage("Le code PIN est obligatoire.")
           .Must(pin =>
           {
               var cleaned = pin?.Replace(" ", "");
               return !string.IsNullOrEmpty(cleaned) &&
                      Regex.IsMatch(cleaned, $@"^\d{{{lengthPin}}}$");
           })
           .WithMessage($"Le code PIN doit contenir exactement {lengthPin} chiffres sans espaces.");
        }
    }
}


