using FluentValidation;

namespace ask.Dtos.Request.auth
{
    public class QueryRefreshToken
    {
        public string? refresh_token { get; set; }

    }

    public class QueryRefreshTokenValidator : AbstractValidator<QueryRefreshToken>
    {
        public QueryRefreshTokenValidator()
        {
            RuleFor(x => x.refresh_token)
                .NotEmpty().WithMessage("Le refresh_token est obligatoire.")
                .NotNull().WithMessage("Le refresh_token ne peut pas être null.");
        }
    }
}


