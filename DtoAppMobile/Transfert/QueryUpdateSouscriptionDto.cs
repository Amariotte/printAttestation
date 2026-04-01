using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryUpdateSouscriptionDto
    {
        public string? motif { get; set; }

    }


    public class QueryUpdateSouscriptionDtoValidator : AbstractValidator<QueryUpdateSouscriptionDto>
    {
        public QueryUpdateSouscriptionDtoValidator()
        {
            RuleFor(x => x.motif)
                .NotEmpty().WithMessage("Le motif est obligatoire.");
        }
    }

}