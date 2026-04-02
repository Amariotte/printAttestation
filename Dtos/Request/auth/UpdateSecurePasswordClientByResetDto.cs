using FluentValidation;


namespace ask.Dtos.Request.auth
{


    public class UpdateSecurePasswordClientByResetDto
    {
        public string? username { get; set; }
        public string? new_password { get; set; }
    }
   


    public class UpdateSecurePasswordClientByResetDtoValidator : AbstractValidator<UpdateSecurePasswordClientByResetDto>
    {

        public UpdateSecurePasswordClientByResetDtoValidator()
        {
            RuleFor(user => user.username)
                .NotEmpty().WithMessage("L'identifiant est requis.");

            RuleFor(user => user.new_password)
                .NotEmpty().WithMessage("Le nouveau mot de passe est requis.");


        }


    }

}
