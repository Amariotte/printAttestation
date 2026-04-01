using FluentValidation;


namespace InteroperabiliteProject.ServicesSecure.Dtos
{
    public class UpdatePasswordClientDto
    {
        public string? old_password { get; set; }
        public string? new_password { get; set; }
    }


    public class UpdatePasswordClientByResetDto
    {
        public string? new_password { get; set; }
    }


    public class InitPasswordClientDto
    {
        public string? identifiant { get; set; }
    }

    public class UpdatePasswordClientDtoValidator : AbstractValidator<UpdatePasswordClientDto>
    {

        public UpdatePasswordClientDtoValidator()
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

    
    public class UpdatePasswordClientByResetDtoValidator : AbstractValidator<UpdatePasswordClientByResetDto>
    {

        public UpdatePasswordClientByResetDtoValidator()
        {

            RuleFor(user => user.new_password)
                .NotEmpty().WithMessage("Le nouveau mot de passe est requis.");


        }

    }

}
