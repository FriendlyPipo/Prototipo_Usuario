using Xunit;
using Users.Application.UserValidations;
using Users.Application.DTO.Request;
using Users.Infrastructure.Exceptions;

namespace Users.Test.Users.Application.Test.Validations
{
    public class UserValidationTest
    {
        private readonly UserValidation _validator;

        public UserValidationTest()
        {
            _validator = new UserValidation();
        }

        [Fact]
        public async Task ValidateRequest_WithInvalidData_ThrowsValidatorException()
        {
            // Arrange
            var invalidUser = new CreateUserDTO
            {
                UserName = "", // Nombre vacío - inválido
                UserLastName = "Do", // Apellido muy corto - inválido
                UserEmail = "invalid-email", // Email inválido
                UserPhoneNumber = "123", // Teléfono muy corto - inválido
                UserDirection = "", // Dirección vacía - inválida
                UserRole = "", // Rol vacío - inválido
                UserPassword = "" // Contraseña vacía - inválida
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidatorException>(
                () => _validator.ValidateRequest(invalidUser)
            );

            // Verificar que el mensaje de error contiene todos los errores esperados
            Assert.Contains("El nombre es requerido", exception.Message);
            Assert.Contains("El apellido no puede exceder 50 caracteres", exception.Message);
            Assert.Contains("El correo no es válido", exception.Message);
            Assert.Contains("El teléfono debe tener 11 dígitos", exception.Message);
            Assert.Contains("La dirección es requerida", exception.Message);
            Assert.Contains("El rol es requerido", exception.Message);
        }

        [Fact]
        public async Task ValidateRequest_WithValidData_ReturnsTrue()
        {
            // Arrange
            var validUser = new CreateUserDTO
            {
                UserName = "John",
                UserLastName = "Doe",
                UserEmail = "john.doe@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "123 Main St",
                UserRole = "Admin",
                UserPassword = "SecurePassword123!" // Contraseña válida
            };

            // Act
            var result = await _validator.ValidateRequest(validUser);

            // Assert
            Assert.True(result);
        }
    }
}
