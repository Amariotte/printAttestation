using FluentValidation;
using System.Text.RegularExpressions;

namespace InteroperabiliteProject.DtoAppMobile.Securite
{
    public class CodePinClientBodyDto
    {
        public string? pin { get; set; }
    }



    public class CodePinClientBodyDtoValidator : AbstractValidator<CodePinClientBodyDto>
    {

        public CodePinClientBodyDtoValidator(int lengthPin)
        {
            RuleFor(user => user.pin)
              .Cascade(CascadeMode.Stop) // Arrête la validation au premier échec
              .NotEmpty().WithMessage("Le code PIN est requis.").NotNull()
              .Must(pin => Regex.IsMatch(pin ?? "", $@"^\d{{{lengthPin}}}$"))
              .WithMessage($"Le code PIN doit contenir exactement {lengthPin} chiffres sans espaces.");
        }


    }




    public class CodePinResetClientBodyDto
    {
        public string? pin { get; set; }
        public string? otp { get; set; }
    }



    public class CodePinResetClientBodyDtoValidator : AbstractValidator<CodePinResetClientBodyDto>
    {

        public CodePinResetClientBodyDtoValidator(int lengthPin)
        {

            RuleFor(user => user.pin)
              .Cascade(CascadeMode.Stop) // Arrête la validation au premier échec
              .NotEmpty().WithMessage("Le code PIN est requis.").NotNull()
              .Must(pin => Regex.IsMatch(pin ?? "", $@"^\d{{{lengthPin}}}$"))
              .WithMessage($"Le code PIN doit contenir exactement {lengthPin} chiffres sans espaces.");



            RuleFor(user => user.otp)
                  .NotEmpty().WithMessage("Le code OTP est obligatoire.");

        }


    }


}
