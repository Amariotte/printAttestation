
using FluentValidation;

namespace InteroperabiliteProject.DtoAppMobile
{
    public class QueryUpdateNotificationDto
    {
        public DateTime? dateLecture { get; set; }

    }


    public class QueryUpdateNotificationDtoValidator : AbstractValidator<QueryUpdateNotificationDto>
    {
        public QueryUpdateNotificationDtoValidator()
        {
            RuleFor(data => data.dateLecture)
              .NotEmpty()
              .WithMessage("La date de lecture est requise.");

        }


    }
}


