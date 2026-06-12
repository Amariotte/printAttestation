using FluentValidation;


namespace ask.Dtos.Request.auth
{
    public class UpdatePasswordUserDto
    {
        public string? old_password { get; set; }
        public string? new_password { get; set; }
    }


    public class InitPasswordUserDto
    {
        public string? email { get; set; }
    }

    public class UpdatePasswordUserDtoValidator : AbstractValidator<UpdatePasswordUserDto>
    {

        public UpdatePasswordUserDtoValidator()
        {

            RuleFor(user => user.old_password)
                   .NotEmpty().WithMessage("L'ancien mot de passe est requis.");

            RuleFor(user => user.new_password)
                .NotEmpty().WithMessage("Le nouveau mot de passe est requis.");

            RuleFor(user => user)
                .Must(user => user.old_password != user.new_password)
                .WithMessage("Le nouveau mot de passe doit être différent de l'ancien mot de passe.");

        }

       
    }

 

}
