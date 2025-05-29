using FluentValidation;
using Users.Application.DTO.Request;
using Users.Infrastructure.Exceptions;

namespace Users.Application.UserValidations
{
    public class UserValidation : AbstractValidator<CreateUserDTO>
    {
        public UserValidation()
        {
                RuleFor(x => x.UserName)
                    .NotEmpty().WithMessage("El nombre es requerido.")
                    .Length(3,50).WithMessage("El nombre no puede exceder 50 caracteres.");

            RuleFor(x => x.UserLastName)
                .NotEmpty().WithMessage("El apellido es requerido.")
                .Length(3,50).WithMessage("El apellido no puede exceder 50 caracteres.");

            RuleFor(x => x.UserEmail)
                .NotEmpty().WithMessage("El correo es requerido.")
                .EmailAddress().WithMessage("El correo no es válido.");

            RuleFor(x => x.UserPhoneNumber)
                .NotEmpty().WithMessage("El teléfono es requerido.")
                .Length(11).WithMessage("El teléfono debe tener 11 dígitos.");

            RuleFor(x => x.UserDirection)
                .NotEmpty().WithMessage("La dirección es requerida.")
                .Length(5, 100).WithMessage("La dirección debe tener entre 5 y 100 caracteres.");

            RuleFor(x => x.UserRole)
                .NotEmpty().WithMessage("El rol es requerido.")
                .Must(role => role == "Postor" || role == "Subastador" || role == "Administrador" || role == "Soporte");
        }
         public virtual async Task<bool> ValidateRequest(CreateUserDTO request)
        {
            var result = await ValidateAsync(request);
            if (!result.IsValid)
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
                throw new ValidatorException(errorMessage);
            }

            return result.IsValid;
        }
    }
}
