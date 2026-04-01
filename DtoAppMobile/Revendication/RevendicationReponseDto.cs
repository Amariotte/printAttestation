using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile.Revendication
{
    public class RevendicationReponseDto
    {
        public bool? decision { get; set; }

    }

    public class RevendicationReponseDtoValidator : AbstractValidator<RevendicationReponseDto>
    {
        public RevendicationReponseDtoValidator()
        {
            RuleFor(x => x.decision)
                .NotNull()
                .WithMessage("La décision doit être spécifiée (true ou false).");
        }
    }
}


