using FluentValidation;

namespace ask.Dtos.Request.auth
{
    public class QueryConfirmationOtpResetPwdDto
    {
        public string otp { get; set; }
        public string challenge { get; set; }
        public string new_password { get; set; }
    }

    public class QueryConfirmationOtpResetPwdDtoValidator : AbstractValidator<QueryConfirmationOtpResetPwdDto>
    {
        public QueryConfirmationOtpResetPwdDtoValidator()
        {
            RuleFor(x => x.otp)
                .NotEmpty().WithMessage("Le code OTP est obligatoire.");

            RuleFor(x => x.challenge)
                .NotEmpty().WithMessage("Le challenge est obligatoire.");

            RuleFor(x => x.new_password)
                .NotEmpty().WithMessage("Le nouveau mot de passe est obligatoire.");
        }
    }
}
