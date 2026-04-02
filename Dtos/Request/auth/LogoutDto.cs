using FluentValidation;

namespace ask.Dtos.Request.auth
{
    public class LogoutDto
    {
        public string? refresh_token { get; set; }
    }

    public class LogoutDtoValidator : AbstractValidator<LogoutDto>
    {
        public LogoutDtoValidator()
        {
            RuleFor(x => x.refresh_token)
                .NotEmpty().WithMessage("Le refresh_token est obligatoire.");
        }
    }
}
