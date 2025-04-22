using FluentValidation;
using UserMs.app.DTO.Request;
using UserMs.Infra.Exceptions;

namespace UserMs.app.UserValidations
{
    public class UserValidation : AbstractValidator<CreateUserDTO>
    {
        public UserValidation()
        {
            RuleFor(x => x.UserNombre)
                .NotEmpty().WithMessage("El nombre es requerido.")
                .Length(50).WithMessage("El nombre no puede exceder 50 caracteres.");

            RuleFor(x => x.UserApellido)
                .NotEmpty().WithMessage("El apellido es requerido.")
                .Length(50).WithMessage("El apellido no puede exceder 50 caracteres.");

            RuleFor(x => x.UserCorreo)
                .NotEmpty().WithMessage("El correo es requerido.")
                .EmailAddress().WithMessage("El correo no es válido.");

            RuleFor(x => x.UserTelefono)
                .NotEmpty().WithMessage("El teléfono es requerido.")
                .Length(11).WithMessage("El teléfono debe tener 11 dígitos.");

            RuleFor(x => x.UserDireccion)
                .NotEmpty().WithMessage("La dirección es requerida.")
                .Length(5, 100).WithMessage("La dirección debe tener entre 5 y 100 caracteres.");

            RuleFor(x => x.UserRol)
                .NotEmpty().WithMessage("El rol es requerido.")
                .Must(rol => rol == "Postor" || rol == "Subastador");
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
