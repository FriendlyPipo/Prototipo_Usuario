using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Users.Application.DTO.Respond;
using Users.Application.Handlers.Queries;
using Users.Application.Queries;
using Users.Core.Repositories;
using Users.Domain.Entities;
using Users.Infrastructure.Exceptions;
using Xunit;

namespace Users.Test.Users.Application.Test.Handlers.Queries
{
    public class GetUserByIdQueryHandlerTests
    {
        private readonly Mock<IUserReadRepository> _mockUserReadRepository;
        private readonly Mock<ILogger<GetUserByIdQueryHandler>> _mockLogger;
        private readonly GetUserByIdQueryHandler _handler;
        private readonly Guid _testUserId;

        public GetUserByIdQueryHandlerTests()
        {
            _mockUserReadRepository = new Mock<IUserReadRepository>();
            _mockLogger = new Mock<ILogger<GetUserByIdQueryHandler>>();
            _handler = new GetUserByIdQueryHandler(_mockUserReadRepository.Object, _mockLogger.Object);
            _testUserId = Guid.NewGuid();
        }

        [Fact]
        public async Task Handle_WithExistingUser_ReturnsUserDTO()
        {
            // Arrange
            var user = new MongoUserDocument
            {
                UserId = _testUserId,
                UserName = "Test",
                UserLastName = "User",
                UserEmail = "test@example.com",
                UserPhoneNumber = "12345678901",
                UserDirection = "Test Address",
                UserRole = "Administrador"
            };

            _mockUserReadRepository.Setup(x => x.GetByIdAsync(_testUserId))
                .ReturnsAsync(user);

            var query = new GetUserByIdQuery(_testUserId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testUserId, result.UserId);
            Assert.Equal(user.UserName, result.UserName);
            Assert.Equal(user.UserLastName, result.UserLastName);
            Assert.Equal(user.UserEmail, result.UserEmail);
            Assert.Equal(user.UserPhoneNumber, result.UserPhoneNumber);
            Assert.Equal(user.UserDirection, result.UserDirection);
            Assert.Equal(user.UserRole, result.UserRole);
        }

        [Fact]
        public async Task Handle_WithNonExistingUser_ThrowsUserException()
        {
            // Arrange
            _mockUserReadRepository.Setup(x => x.GetByIdAsync(_testUserId))
                .ReturnsAsync((MongoUserDocument)null);

            var query = new GetUserByIdQuery(_testUserId);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(query, CancellationToken.None));

            Assert.Equal("Error al obtener usuario por ID", exception.Message);
            Assert.IsType<UserNotFoundException>(exception.InnerException);
            Assert.Equal("Usuario no encontrado", exception.InnerException.Message);
        }

        [Fact]
        public async Task Handle_WhenRepositoryThrowsException_ThrowsUserException()
        {
            // Arrange
            _mockUserReadRepository.Setup(x => x.GetByIdAsync(_testUserId))
                .ThrowsAsync(new Exception("Database error"));

            var query = new GetUserByIdQuery(_testUserId);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(query, CancellationToken.None));

            Assert.Equal("Error al obtener usuario por ID", exception.Message);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((o, t) => true)), 
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithInvalidGuid_ThrowsUserException()
        {
            // Arrange
            var query = new GetUserByIdQuery(Guid.Empty);

            _mockUserReadRepository.Setup(x => x.GetByIdAsync(Guid.Empty))
                .ReturnsAsync((MongoUserDocument)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(query, CancellationToken.None));

            Assert.Equal("Error al obtener usuario por ID", exception.Message);
            Assert.IsType<UserNotFoundException>(exception.InnerException);
            Assert.Equal("Usuario no encontrado", exception.InnerException.Message);
        }
    }
}
