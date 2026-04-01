using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryConfirmationOtpDto
    {
             public string? otp { get; set; }
      
    }


    public class QueryConfirmationOtpResetPwdDto : QueryConfirmationOtpDto
    {
        public string? challenge { get; set; }
        public string? new_password { get; set; }

    }


    public class QueryConfirmationOtpRegisterDto : QueryConfirmationOtpDto
    {
        public string? challenge { get; set; }
    }


    public class QueryConfirmationOtpRegisterDtoValidator : AbstractValidator<QueryConfirmationOtpRegisterDto>
    {
        public QueryConfirmationOtpRegisterDtoValidator()
        {
            Include(new QueryConfirmationOtpDtoValidator());

            RuleFor(x => x.challenge)
                .NotEmpty().WithMessage("Le challenge est obligatoire.");

        }
    }

    public class QueryConfirmationOtpDtoValidator : AbstractValidator<QueryConfirmationOtpDto>
    {
        public QueryConfirmationOtpDtoValidator()
        {
            RuleFor(x => x.otp)
                .NotEmpty().WithMessage("Le code OTP est obligatoire.");
        }
    }


    public class QueryConfirmationOtpResetPwdDtoValidator : AbstractValidator<QueryConfirmationOtpResetPwdDto>
    {
        public QueryConfirmationOtpResetPwdDtoValidator()
        {
            Include(new QueryConfirmationOtpDtoValidator());

            RuleFor(x => x.challenge)
                .NotEmpty().WithMessage("Le challenge est obligatoire.");

            RuleFor(x => x.new_password)
               .NotEmpty().WithMessage("Le nouveau mot de passe est obligatoire.");
        }
    }
}
