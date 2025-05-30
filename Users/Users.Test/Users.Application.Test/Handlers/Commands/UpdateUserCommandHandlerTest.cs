using Moq;
using Xunit;
using MediatR;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Users.Domain.Entities;
using Users.Application.Commands;
using Users.Infrastructure.Database;
using Users.Core.Repositories;
using Users.Infrastructure.Exceptions;
using Users.Application.Handlers.Commands;
using Users.Infrastructure.EventBus;
using Users.Core.Events;
using Users.Application.DTO.Request;
using System.Linq.Expressions;
using Users.Infrastructure.EventBus.Events;
using System.Linq;
using System.Text.Json;

namespace Users.Test.Application.Handlers.Commands
{
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;
        private readonly List<TEntity> _data;

        internal TestAsyncQueryProvider(IQueryProvider inner, IEnumerable<TEntity> data)
        {
            _inner = inner;
            _data = data.ToList();
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression, _data);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression, _data.Cast<TElement>());
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var elementType = typeof(TResult).GetGenericArguments()[0];
            var queryable = _data.AsQueryable();
            
            var resultExpression = expression as MethodCallExpression;
            if (resultExpression != null && resultExpression.Method.Name == "FirstOrDefault")
            {
                var result = queryable.Provider.Execute(expression);
                return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                    .MakeGenericMethod(elementType)
                    .Invoke(null, new[] { result });
            }

            return Execute<TResult>(expression);
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        private readonly List<T> _data;

        public TestAsyncEnumerable(Expression expression, IEnumerable<T> data)
            : base(expression)
        {
            _data = data.ToList();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(_data.GetEnumerator());
        }

        IQueryProvider IQueryable.Provider
        {
            get { return new TestAsyncQueryProvider<T>(_data.AsQueryable().Provider, _data); }
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current
        {
            get { return _inner.Current; }
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return new ValueTask();
        }
    }

    public class UpdateUserCommandHandlerTest
    {
        private readonly Mock<IUserWriteRepository> _mockUserWriteRepository;
        private readonly Mock<IKeycloakRepository> _mockKeycloakRepository;
        private readonly Mock<IEventBus> _mockEventBus;
        private readonly Mock<ILogger<UpdateUserCommandHandler>> _mockLogger;
        private readonly Mock<DbSet<UserRole>> _mockRoleDbSet;
        private readonly UpdateUserCommandHandler _handler;
        private readonly Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> _mockTransaction;
        private readonly Mock<DatabaseFacade> _mockDatabase;
        private readonly Mock<UserDbContext> _dbContextMock;

        public UpdateUserCommandHandlerTest()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _mockUserWriteRepository = new Mock<IUserWriteRepository>();
            _mockKeycloakRepository = new Mock<IKeycloakRepository>();
            _mockEventBus = new Mock<IEventBus>();
            _mockLogger = new Mock<ILogger<UpdateUserCommandHandler>>();
            _mockRoleDbSet = new Mock<DbSet<UserRole>>();
            _mockTransaction = new Mock<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
            _dbContextMock = new Mock<UserDbContext>(options);
            
            // Set up the async provider for the mock DbSet
            _mockRoleDbSet.As<IAsyncEnumerable<UserRole>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<UserRole>(new List<UserRole>().GetEnumerator()));

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<UserRole>(new List<UserRole>().AsQueryable().Provider, new List<UserRole>()));

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Expression)
                .Returns(new List<UserRole>().AsQueryable().Expression);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.ElementType)
                .Returns(new List<UserRole>().AsQueryable().ElementType);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => new List<UserRole>().GetEnumerator());

            _mockDatabase = new Mock<DatabaseFacade>(_dbContextMock.Object);
            _mockDatabase.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockTransaction.Object);

            _dbContextMock.Setup(x => x.Database).Returns(_mockDatabase.Object);
            _dbContextMock.Setup(x => x.Role).Returns(_mockRoleDbSet.Object);

            _handler = new UpdateUserCommandHandler(
                _mockEventBus.Object,
                _mockUserWriteRepository.Object,
                _dbContextMock.Object,
                _mockKeycloakRepository.Object,
                _mockLogger.Object
            );
        }

        private void SetupRoleDbSet(UserRole existingRole)
        {
            var roles = new List<UserRole> { existingRole };
            var queryable = roles.AsQueryable();

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<UserRole>(queryable.Provider, roles));

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Expression)
                .Returns(queryable.Expression);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.ElementType)
                .Returns(queryable.ElementType);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator);

            _dbContextMock.Setup(x => x.Role)
                .Returns(_mockRoleDbSet.Object);

            _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesUserSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(
                "Test",
                "User",
                "test@test.com",
                "12345678901",
                "Test Address"
            );
            var existingRole = new UserRole(UserRoleName.Postor) { UserId = existingUser.UserId };
            existingUser.UserRoles.Add(existingRole);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            var roles = new List<UserRole> { existingRole };
            var queryable = roles.AsQueryable();

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<UserRole>(queryable.Provider, roles));

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Expression)
                .Returns(queryable.Expression);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.ElementType)
                .Returns(queryable.ElementType);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator);

            _dbContextMock.Setup(x => x.Role)
                .Returns(_mockRoleDbSet.Object);

            _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var updateRequest = new UpdateUserDTO
            {
                UserId = userId,
                UserEmail = "updated@test.com",
                UserName = "Updated",
                UserLastName = "User",
                UserPhoneNumber = "09876543211",
                UserDirection = "Updated Address",
                UserRole = "Administrador"
            };

            var command = new UpdateUserCommand(updateRequest);

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync("token");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("keycloakUserId");
            _mockKeycloakRepository.Setup(x => x.UpdateUserAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("User updated successfully");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal("Usuario Actualizado Correctamente", result);
            _mockRoleDbSet.Verify(x => x.Remove(It.IsAny<UserRole>()), Times.Once);
            _mockRoleDbSet.Verify(x => x.Add(It.IsAny<UserRole>()), Times.Once);
            _mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidRole_ThrowsUserRoleException()
        {
            // Arrange
            var updateRequest = new UpdateUserDTO
            {
                UserId = Guid.NewGuid(),
                UserEmail = "test@test.com",
                UserRole = "InvalidRole"
            };

            var command = new UpdateUserCommand(updateRequest);

            // Act & Assert
            await Assert.ThrowsAsync<UserRoleException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_SameRole_DoesNotUpdateRole()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(
                "Test",
                "User",
                "test@test.com",
                "12345678901",
                "Test Address"
            );
            var existingRole = new UserRole(UserRoleName.Administrador) { UserId = existingUser.UserId };
            existingUser.UserRoles.Add(existingRole);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            var roles = new List<UserRole> { existingRole };
            var queryable = roles.AsQueryable();

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<UserRole>(queryable.Provider, roles));

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Expression)
                .Returns(queryable.Expression);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.ElementType)
                .Returns(queryable.ElementType);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator);

            _dbContextMock.Setup(x => x.Role)
                .Returns(_mockRoleDbSet.Object);

            _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var updateRequest = new UpdateUserDTO
            {
                UserId = userId,
                UserEmail = "test@test.com",
                UserRole = "Administrador"
            };

            var command = new UpdateUserCommand(updateRequest);

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync("token");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("keycloakUserId");
            _mockKeycloakRepository.Setup(x => x.UpdateUserAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("User updated successfully");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal("Usuario Actualizado Correctamente", result);
            _mockRoleDbSet.Verify(x => x.Remove(It.IsAny<UserRole>()), Times.Never);
            _mockRoleDbSet.Verify(x => x.Add(It.IsAny<UserRole>()), Times.Never);
            _mockTransaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DatabaseError_RollsBackTransaction()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(
                "Test",
                "User",
                "test@test.com",
                "12345678901",
                "Test Address"
            );
            var existingRole = new UserRole(UserRoleName.Postor) { UserId = existingUser.UserId };
            existingUser.UserRoles.Add(existingRole);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _mockUserWriteRepository.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ThrowsAsync(new Exception("Database error"));

            var updateRequest = new UpdateUserDTO
            {
                UserId = userId,
                UserEmail = "test@test.com",
                UserRole = "Administrador"
            };

            var command = new UpdateUserCommand(updateRequest);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(command, CancellationToken.None));

            _mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithPassword_UpdatesUserAndKeycloakCredentials()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(
                "Test",
                "User",
                "test@test.com",
                "12345678901",
                "Test Address"
            );
            var existingRole = new UserRole(UserRoleName.Postor) { UserId = existingUser.UserId };
            existingUser.UserRoles.Add(existingRole);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            var roles = new List<UserRole> { existingRole };
            var queryable = roles.AsQueryable();

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<UserRole>(queryable.Provider, roles));

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Expression)
                .Returns(queryable.Expression);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.ElementType)
                .Returns(queryable.ElementType);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator);

            _dbContextMock.Setup(x => x.Role)
                .Returns(_mockRoleDbSet.Object);

            _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var updateRequest = new UpdateUserDTO
            {
                UserId = userId,
                UserEmail = "updated@test.com",
                UserName = "Updated",
                UserLastName = "User",
                UserPhoneNumber = "09876543211",
                UserDirection = "Updated Address",
                UserRole = "Administrador",
                UserPassword = "newPassword123"
            };

            var command = new UpdateUserCommand(updateRequest);

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync("token");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("keycloakUserId");

            object expectedKeycloakUser = new
            {
                username = updateRequest.UserEmail,
                email = updateRequest.UserEmail,
                enabled = true,
                firstName = updateRequest.UserName ?? existingUser.UserName,
                lastName = updateRequest.UserLastName ?? existingUser.UserLastName,
                credentials = new[] { new { type = "password", value = updateRequest.UserPassword, temporary = false } }
            };

            _mockKeycloakRepository.Setup(x => x.UpdateUserAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync("User updated successfully")
                .Callback<object, string, string>((user, userId, token) => {
                    var userJson = JsonSerializer.Serialize(user);
                    var expectedJson = JsonSerializer.Serialize(expectedKeycloakUser);
                    Assert.Equal(expectedJson, userJson);
                    Assert.Equal("keycloakUserId", userId);
                    Assert.Equal("token", token);
                });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal("Usuario Actualizado Correctamente", result);
            _mockKeycloakRepository.Verify(x => x.UpdateUserAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithoutPassword_UpdatesUserWithoutCredentials()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(
                "Test",
                "User",
                "test@test.com",
                "12345678901",
                "Test Address"
            );
            var existingRole = new UserRole(UserRoleName.Postor) { UserId = existingUser.UserId };
            existingUser.UserRoles.Add(existingRole);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            var roles = new List<UserRole> { existingRole };
            var queryable = roles.AsQueryable();

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<UserRole>(queryable.Provider, roles));

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Expression)
                .Returns(queryable.Expression);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.ElementType)
                .Returns(queryable.ElementType);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator);

            _dbContextMock.Setup(x => x.Role)
                .Returns(_mockRoleDbSet.Object);

            _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var updateRequest = new UpdateUserDTO
            {
                UserId = userId,
                UserEmail = "updated@test.com",
                UserName = "Updated",
                UserLastName = "User",
                UserPhoneNumber = "09876543211",
                UserDirection = "Updated Address",
                UserRole = "Administrador",
                UserPassword = null
            };

            var command = new UpdateUserCommand(updateRequest);

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync("token");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("keycloakUserId");

            object expectedKeycloakUser = new
            {
                username = updateRequest.UserEmail,
                email = updateRequest.UserEmail,
                enabled = true,
                firstName = updateRequest.UserName ?? existingUser.UserName,
                lastName = updateRequest.UserLastName ?? existingUser.UserLastName
            };

            _mockKeycloakRepository.Setup(x => x.UpdateUserAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync("User updated successfully")
                .Callback<object, string, string>((user, userId, token) => {
                    var userJson = JsonSerializer.Serialize(user);
                    var expectedJson = JsonSerializer.Serialize(expectedKeycloakUser);
                    Assert.Equal(expectedJson, userJson);
                    Assert.Equal("keycloakUserId", userId);
                    Assert.Equal("token", token);
                });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal("Usuario Actualizado Correctamente", result);
            _mockKeycloakRepository.Verify(x => x.UpdateUserAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_KeycloakUpdateError_LogsAndRethrowsException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = new User(
                "Test",
                "User",
                "test@test.com",
                "12345678901",
                "Test Address"
            );
            var existingRole = new UserRole(UserRoleName.Postor) { UserId = existingUser.UserId };
            existingUser.UserRoles.Add(existingRole);

            _mockUserWriteRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            var roles = new List<UserRole> { existingRole };
            var queryable = roles.AsQueryable();

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<UserRole>(queryable.Provider, roles));

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.Expression)
                .Returns(queryable.Expression);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.ElementType)
                .Returns(queryable.ElementType);

            _mockRoleDbSet.As<IQueryable<UserRole>>()
                .Setup(m => m.GetEnumerator())
                .Returns(roles.GetEnumerator);

            _dbContextMock.Setup(x => x.Role)
                .Returns(_mockRoleDbSet.Object);

            _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var updateRequest = new UpdateUserDTO
            {
                UserId = userId,
                UserEmail = "updated@test.com",
                UserName = "Updated",
                UserLastName = "User",
                UserPhoneNumber = "09876543211",
                UserDirection = "Updated Address",
                UserRole = "Administrador"
            };

            var command = new UpdateUserCommand(updateRequest);

            _mockKeycloakRepository.Setup(x => x.GetTokenAsync())
                .ReturnsAsync("token");
            _mockKeycloakRepository.Setup(x => x.GetUserIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("keycloakUserId");

            var keycloakException = new KeycloakException("Error updating user in Keycloak");
            _mockKeycloakRepository.Setup(x => x.UpdateUserAsync(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ThrowsAsync(keycloakException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeycloakException>(() => 
                _handler.Handle(command, CancellationToken.None));

            Assert.Same(keycloakException, exception);

            // Verify first log (specific Keycloak error)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error al actualizar usuario en Keycloak")),
                    It.Is<KeycloakException>(ex => ex == keycloakException),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            // Verify second log (general error handling)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error durante la actualización del usuario")),
                    It.Is<KeycloakException>(ex => ex == keycloakException),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockTransaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
