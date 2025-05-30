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
    public class CreateUserCommandHandlerTest
    {
        private readonly Mock<IEventBus> _mockEventBus;
        private readonly Mock<IUserWriteRepository> _mockUserWriteRepository;
        private readonly Mock<UserDbContext> _mockDbContext;
        private readonly Mock<IKeycloakRepository> _mockKeycloakRepository;
        private readonly Mock<ILogger<CreateUserCommandHandler>> _mockLogger;
        private readonly Mock<DbSet<UserRole>> _mockRoleDbSet;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly CreateUserCommandHandler _handler;

        public CreateUserCommandHandlerTest()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockEventBus = new Mock<IEventBus>();
            _mockUserWriteRepository = new Mock<IUserWriteRepository>();
            _mockDbContext = new Mock<UserDbContext>(options);
            _mockKeycloakRepository = new Mock<IKeycloakRepository>();
            _mockLogger = new Mock<ILogger<CreateUserCommandHandler>>();
            _mockRoleDbSet = new Mock<DbSet<UserRole>>();
            _mockTransaction = new Mock<IDbContextTransaction>();

            _mockDbContext.Setup(x => x.Role).Returns(_mockRoleDbSet.Object);
            
            // Setup the transaction
            var mockDatabase = new Mock<DatabaseFacade>(_mockDbContext.Object);
            mockDatabase.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockTransaction.Object);
            _mockDbContext.Setup(x => x.Database).Returns(mockDatabase.Object);

            _handler = new CreateUserCommandHandler(
                _mockEventBus.Object,
                _mockUserWriteRepository.Object,
                _mockDbContext.Object,
                _mockKeycloakRepository.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateUserSuccessfully()
        {
            // Arrange
            var command = new CreateUserCommand(new CreateUserDTO
            {
                UserName = "Test",
                UserLastName = "User",
                UserEmail = "Test@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "123 Main St",
                UserPassword = "Password123!",
                UserRole = "Administrador"
            });

            var keycloakToken = "fake-token";
            var keycloakUserId =  "kc-user-id";

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync(keycloakToken);
            _mockKeycloakRepository.Setup(x => x.CreateUserAsync(It.IsAny<object>(), keycloakToken))
                .ReturnsAsync("{}");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(command.Users.UserEmail, keycloakToken))
                .ReturnsAsync(keycloakUserId);
            _mockKeycloakRepository.Setup(x => x.SendVerificationEmailAsync(keycloakUserId, keycloakToken))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty.ToString(), result);

            _mockUserWriteRepository.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
            _mockEventBus.Verify(x => x.Publish(It.IsAny<UserCreatedEvent>(), "user.created"), Times.Once);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidRole_ShouldThrowUserRoleException()
        {
            // Arrange
            var command = new CreateUserCommand(new CreateUserDTO
            {
                UserName = "Test",
                UserLastName = "User",
                UserEmail = "Test@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "123 Main St",
                UserPassword = "Password123!",
                UserRole = "norole"
            });

            // Act & Assert
            await Assert.ThrowsAsync<UserRoleException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_KeycloakError_ShouldRollbackAndThrowKeycloakException()
        {
            // Arrange
            var command = new CreateUserCommand(new CreateUserDTO
            {
                UserName = "Test",
                UserLastName = "User",
                UserEmail = "Test@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "123 Main St",
                UserPassword = "Password123!",
                UserRole = "Administrador"
            });

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ThrowsAsync(new KeycloakException("Error al obtener token de Keycloak"));

            // Act & Assert
            await Assert.ThrowsAsync<KeycloakException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_DbUpdateError_ShouldRollbackAndThrowUserException()
        {
            // Arrange
            var command = new CreateUserCommand(new CreateUserDTO
            {
                UserName = "Test",
                UserLastName = "User",
                UserEmail = "Test@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "123 Main St",
                UserPassword = "Password123!",
                UserRole = "Administrador"
            });

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB error", new Exception()));

            // Act & Assert
            await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_EmailVerificationError_ShouldRollbackAndThrowKeycloakException()
        {
            // Arrange
            var command = new CreateUserCommand(new CreateUserDTO
            {
                UserName = "Test",
                UserLastName = "User",
                UserEmail = "Test@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "123 Main St",
                UserPassword = "Password123!",
                UserRole = "Administrador"
            });

            var keycloakToken = "fake-token";
            var keycloakUserId = "kc-user-id";

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync(keycloakToken);
            _mockKeycloakRepository.Setup(x => x.CreateUserAsync(It.IsAny<object>(), keycloakToken))
                .ReturnsAsync("{}");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(command.Users.UserEmail, keycloakToken))
                .ReturnsAsync(keycloakUserId);
            _mockKeycloakRepository.Setup(x => x.SendVerificationEmailAsync(keycloakUserId, keycloakToken))
                .ThrowsAsync(new Exception("Email verification error"));

            // Act & Assert
            await Assert.ThrowsAsync<KeycloakException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }
    }
}
