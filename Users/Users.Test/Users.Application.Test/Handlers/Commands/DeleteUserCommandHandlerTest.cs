using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Users.Application.Commands;
using Users.Application.DTO.Request;
using Users.Infrastructure.EventBus.Events;
using Users.Application.Handlers.Commands;
using Users.Core.Events;
using Users.Core.Repositories;
using Users.Domain.Entities;
using Users.Infrastructure.Database;
using Users.Infrastructure.EventBus;
using Users.Infrastructure.Exceptions;
using Xunit;

namespace Users.Test.Users.Application.Test.Handlers.Commands
{
    public class DeleteUserCommandHandlerTest
    {
        private readonly Mock<IEventBus> _mockEventBus;
        private readonly Mock<IUserWriteRepository> _mockUserWriteRepository;
        private readonly Mock<UserDbContext> _mockDbContext;
        private readonly Mock<IKeycloakRepository> _mockKeycloakRepository;
        private readonly Mock<ILogger<DeleteUserCommandHandler>> _mockLogger;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly DeleteUserCommandHandler _handler;

        public DeleteUserCommandHandlerTest()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockEventBus = new Mock<IEventBus>();
            _mockUserWriteRepository = new Mock<IUserWriteRepository>();
            _mockDbContext = new Mock<UserDbContext>(options);
            _mockKeycloakRepository = new Mock<IKeycloakRepository>();
            _mockLogger = new Mock<ILogger<DeleteUserCommandHandler>>();
            _mockTransaction = new Mock<IDbContextTransaction>();

            // Setup the transaction
            var mockDatabase = new Mock<DatabaseFacade>(_mockDbContext.Object);
            mockDatabase.Setup(d => d.BeginTransactionAsync(CancellationToken.None))
                .ReturnsAsync(_mockTransaction.Object);
            _mockDbContext.Setup(x => x.Database).Returns(mockDatabase.Object);

            _handler = new DeleteUserCommandHandler(
                _mockEventBus.Object,
                _mockUserWriteRepository.Object,
                _mockDbContext.Object,
                _mockKeycloakRepository.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldDeleteUserSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new DeleteUserCommand(new DeleteUserDTO { UserId = userId });
            var user = CreateTestUser(userId);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserWriteRepository.Setup(x => x.DeleteAsync(userId))
                .Returns(Task.CompletedTask);
            _mockDbContext.Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(1);

            var keycloakToken = "fake-token";
            var keycloakUserId = "kc-user-id";

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync(keycloakToken);
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(user.UserEmail, keycloakToken))
                .ReturnsAsync(keycloakUserId);
            _mockKeycloakRepository.Setup(x => x.DisableUserAsync(keycloakUserId, keycloakToken))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal("Usuario eliminado correctamente.", result);
            _mockUserWriteRepository.Verify(x => x.DeleteAsync(userId), Times.Once);
            _mockEventBus.Verify(x => x.Publish(It.IsAny<UserDeletedEvent>(), "user.deleted"), Times.Once);
            _mockTransaction.Verify(x => x.CommitAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotFound_ShouldThrowUserNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new DeleteUserCommand(new DeleteUserDTO { UserId = userId });

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act & Assert
            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_DbUpdateError_ShouldRollbackAndThrowUserException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new DeleteUserCommand(new DeleteUserDTO { UserId = userId });
            var user = CreateTestUser(userId);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockDbContext.Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ThrowsAsync(new DbUpdateException("DB error", new Exception()));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.Contains("Error al eliminar el usuario de la base de datos", exception.Message);
            _mockTransaction.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_KeycloakUserNotFound_ShouldRollbackAndThrowKeycloakException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new DeleteUserCommand(new DeleteUserDTO { UserId = userId });
            var user = CreateTestUser(userId);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockDbContext.Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(1);

            var keycloakToken = "fake-token";
            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync(keycloakToken);
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(user.UserEmail, keycloakToken))
                .ReturnsAsync((string)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeycloakException>(() =>
                _handler.Handle(command, CancellationToken.None));
            Assert.Contains("no encontrado en Keycloak", exception.Message);
            _mockTransaction.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_KeycloakDisableError_ShouldRollbackAndThrowKeycloakException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new DeleteUserCommand(new DeleteUserDTO { UserId = userId });
            var user = CreateTestUser(userId);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockDbContext.Setup(x => x.SaveChangesAsync(CancellationToken.None))
                .ReturnsAsync(1);

            var keycloakToken = "fake-token";
            var keycloakUserId = "kc-user-id";

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync(keycloakToken);
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(user.UserEmail, keycloakToken))
                .ReturnsAsync(keycloakUserId);
            _mockKeycloakRepository.Setup(x => x.DisableUserAsync(keycloakUserId, keycloakToken))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeycloakException>(() =>
                _handler.Handle(command, CancellationToken.None));
            Assert.Contains("Fallo al deshabilitar usuario en Keycloak", exception.Message);
            _mockTransaction.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_UnexpectedError_ShouldRollbackAndThrowUserException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new DeleteUserCommand(new DeleteUserDTO { UserId = userId });
            var user = CreateTestUser(userId);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserWriteRepository.Setup(x => x.DeleteAsync(userId))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(command, CancellationToken.None));
            Assert.Contains("Error inesperado al procesar la eliminación del usuario", exception.Message);
            _mockTransaction.Verify(x => x.RollbackAsync(CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_NoRowsAffected_ShouldThrowUserException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new DeleteUserCommand(new DeleteUserDTO { UserId = userId });
            var user = CreateTestUser(userId);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Configurar el mock para que DeleteAsync lance la excepción específica
            _mockUserWriteRepository.Setup(x => x.DeleteAsync(userId))
                .Returns(Task.CompletedTask);

            // Mock SaveChangesAsync para retornar 0 filas afectadas
            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Mock de la transacción para que no lance excepciones
            _mockTransaction.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() => 
                _handler.Handle(command, CancellationToken.None));

            // Verificar el mensaje de error específico
            Assert.Contains("No se pudo confirmar la eliminación del usuario de la base de datos (0 filas afectadas)", exception.Message);

            // Verificar que se intentó hacer rollback
            _mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verificar que no se llegó a publicar el evento
            _mockEventBus.Verify(x => x.Publish(It.IsAny<UserDeletedEvent>(), It.IsAny<string>()), Times.Never);

            // Verificar que no se intentó acceder a Keycloak
            _mockKeycloakRepository.Verify(x => x.GetTokenAsync(), Times.Never);
            _mockKeycloakRepository.Verify(x => x.GetUserIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockKeycloakRepository.Verify(x => x.DisableUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private User CreateTestUser(Guid userId)
        {
            // Use reflection to set the UserId after creation
            var user = new User("Test", "User", "test@example.com", "12345678901", "Test Address");
            var field = typeof(User).GetProperty("UserId");
            field.SetValue(user, userId);
            return user;
        }
    }
}
