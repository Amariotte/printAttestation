using FluentValidation;
using System.Text.RegularExpressions;

namespace ask.Dtos.Request.auth
{
    public class UpdateCodePinClientDto
    {
        public string? old_pin { get; set; }
        public string? new_pin { get; set; }
    }



    public class UpdateCodePinClientDtoValidator : AbstractValidator<UpdateCodePinClientDto>
    {

        public UpdateCodePinClientDtoValidator(int lengthPin)
        {
            RuleFor(user => user.old_pin)
              .Cascade(CascadeMode.Stop) // Arrête la validation au premier échec
              .NotEmpty().WithMessage("L'ancien code PIN est requis.").NotNull()
              .Must(pin => Regex.IsMatch(pin ?? "", $@"^\d{{{lengthPin}}}$"))
              .WithMessage($"L'ancien code PIN doit contenir exactement {lengthPin} chiffres sans espaces.");

            RuleFor(user => user.new_pin)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Le nouveau code PIN est requis.").NotNull()
                .Must(pin => Regex.IsMatch(pin ?? "", $@"^\d{{{lengthPin}}}$"))
                .WithMessage($"Le nouveau code PIN doit contenir exactement {lengthPin} chiffres sans espaces.");

            RuleFor(user => user)
                .Must(user => user.old_pin != user.new_pin)
                .WithMessage("Le nouveau code PIN doit être différent de l'ancien code PIN.");
        }


    }


    public class UpdateCodePinSecureClientDtoValidator : AbstractValidator<UpdateCodePinClientDto>
    {

        public UpdateCodePinSecureClientDtoValidator()
        {

            {
                RuleFor(user => user.old_pin)
                    .NotEmpty().WithMessage("L'ancien code PIN est requis.");

                RuleFor(user => user.new_pin)
                    .NotEmpty().WithMessage("Le nouveau code PIN est requis.");

                RuleFor(user => user)
                    .Must(user =>
                        !string.IsNullOrEmpty(user.old_pin) &&
                        !string.IsNullOrEmpty(user.new_pin) &&
                        user.old_pin != user.new_pin)
                    .WithMessage("Le nouveau code PIN doit être différent de l'ancien.");
            }
        }
    }

}
