using FluentValidation;

namespace ask.Dtos.Request.auth
{
    public class QueryConfirmationOtpRegisterDto
    {
        public string otp { get; set; }
        public string challenge { get; set; }
    }

    public class QueryConfirmationOtpRegisterDtoValidator : AbstractValidator<QueryConfirmationOtpRegisterDto>
    {
        public QueryConfirmationOtpRegisterDtoValidator()
        {
            RuleFor(x => x.otp)
                .NotEmpty().WithMessage("Le code OTP est obligatoire.");

            RuleFor(x => x.challenge)
                .NotEmpty().WithMessage("Le challenge est obligatoire.");
        }
    }
}
