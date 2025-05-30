using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Users.Application.Commands;
using Users.Application.DTO.Request;
using Users.Application.Handlers.Commands;
using Users.Core.Repositories;
using Users.Infrastructure.Exceptions;
using Xunit;

namespace Users.Test.Users.Application.Test.Handlers.Commands
{
    public class ForgotPasswordCommandHandlerTests
    {
        private readonly Mock<IKeycloakRepository> _mockKeycloakRepository;
        private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _mockLogger;
        private readonly ForgotPasswordCommandHandler _handler;

        public ForgotPasswordCommandHandlerTests()
        {
            _mockKeycloakRepository = new Mock<IKeycloakRepository>();
            _mockLogger = new Mock<ILogger<ForgotPasswordCommandHandler>>();
            _handler = new ForgotPasswordCommandHandler(_mockKeycloakRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidEmail_ReturnsSuccessMessage()
        {
            // Arrange
            var email = "test@example.com";
            var request = new ForgotPasswordCommand(new ForgotPasswordDTO { UserEmail = email });

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync("token");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(email, It.IsAny<string>()))
                .ReturnsAsync("user-id");

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Equal("Recibirás un enlace para restablecer tu contraseña.", result);
            _mockKeycloakRepository.Verify(x => x.SendPasswordResetEmailAsync("user-id", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EmailNotFound_ThrowsException()
        {
            // Arrange
            var email = "notfound@example.com";
            var request = new ForgotPasswordCommand(new ForgotPasswordDTO { UserEmail = email });

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync("token");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(email, It.IsAny<string>()))
                .ReturnsAsync((string)null);
            _mockKeycloakRepository.Setup(x => x.SendPasswordResetEmailAsync(null, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Error sending email to null user"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeycloakException>(() => 
                _handler.Handle(request, CancellationToken.None));
            Assert.Equal("Error al envíar email de restablecimiento de contraseña", exception.Message);
        }

        [Fact]
        public async Task Handle_SendEmailFails_ThrowsException()
        {
            // Arrange
            var email = "test@example.com";
            var request = new ForgotPasswordCommand(new ForgotPasswordDTO { UserEmail = email });

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync("token");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(email, It.IsAny<string>()))
                .ReturnsAsync("user-id");
            _mockKeycloakRepository.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Error sending email"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeycloakException>(() => 
                _handler.Handle(request, CancellationToken.None));
            Assert.Equal("Error al envíar email de restablecimiento de contraseña", exception.Message);
        }
    }
}
