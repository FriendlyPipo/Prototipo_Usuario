using System;
using System.Collections.Generic;
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
    public class GetAllUsersQueryHandlerTests
    {
        private readonly Mock<IUserReadRepository> _mockUserReadRepository;
        private readonly Mock<ILogger<GetAllUsersQueryHandler>> _mockLogger;
        private readonly GetAllUsersQueryHandler _handler;

        public GetAllUsersQueryHandlerTests()
        {
            _mockUserReadRepository = new Mock<IUserReadRepository>();
            _mockLogger = new Mock<ILogger<GetAllUsersQueryHandler>>();
            _handler = new GetAllUsersQueryHandler(_mockUserReadRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithExistingUsers_ReturnsUserDTOList()
        {
            // Arrange
            var users = new List<MongoUserDocument>
            {
                new MongoUserDocument
                {
                    UserId = Guid.NewGuid(),
                    UserName = "Test1",
                    UserLastName = "User1",
                    UserEmail = "test1@example.com",
                    UserPhoneNumber = "12345678901",
                    UserDirection = "Test Address 1",
                    UserRole = "Administrador"
                },
                new MongoUserDocument
                {
                    UserId = Guid.NewGuid(),
                    UserName = "Test2",
                    UserLastName = "User2",
                    UserEmail = "test2@example.com",
                    UserPhoneNumber = "12345678902",
                    UserDirection = "Test Address 2",
                    UserRole = "Postor"
                }
            };

            _mockUserReadRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(users);

            var query = new GetAllUsersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Collection(result,
                user1 =>
                {
                    Assert.Equal(users[0].UserId, user1.UserId);
                    Assert.Equal(users[0].UserName, user1.UserName);
                    Assert.Equal(users[0].UserLastName, user1.UserLastName);
                    Assert.Equal(users[0].UserEmail, user1.UserEmail);
                    Assert.Equal(users[0].UserPhoneNumber, user1.UserPhoneNumber);
                    Assert.Equal(users[0].UserDirection, user1.UserDirection);
                    Assert.Equal(users[0].UserRole, user1.UserRole);
                },
                user2 =>
                {
                    Assert.Equal(users[1].UserId, user2.UserId);
                    Assert.Equal(users[1].UserName, user2.UserName);
                    Assert.Equal(users[1].UserLastName, user2.UserLastName);
                    Assert.Equal(users[1].UserEmail, user2.UserEmail);
                    Assert.Equal(users[1].UserPhoneNumber, user2.UserPhoneNumber);
                    Assert.Equal(users[1].UserDirection, user2.UserDirection);
                    Assert.Equal(users[1].UserRole, user2.UserRole);
                });
        }

        [Fact]
        public async Task Handle_WithNoUsers_ThrowsUserNotFoundException()
        {
            // Arrange
            _mockUserReadRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<MongoUserDocument>());

            var query = new GetAllUsersQuery();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(query, CancellationToken.None));

            Assert.Equal("Error al obtener todos los usuarios", exception.Message);
            Assert.IsType<UserNotFoundException>(exception.InnerException);
            Assert.Equal("No se encontraron usuarios", exception.InnerException.Message);
        }

        [Fact]
        public async Task Handle_WithNullUsers_ThrowsUserNotFoundException()
        {
            // Arrange
            _mockUserReadRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync((List<MongoUserDocument>)null);

            var query = new GetAllUsersQuery();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(query, CancellationToken.None));

            Assert.Equal("Error al obtener todos los usuarios", exception.Message);
            Assert.IsType<UserNotFoundException>(exception.InnerException);
            Assert.Equal("No se encontraron usuarios", exception.InnerException.Message);
        }

        [Fact]
        public async Task Handle_WhenRepositoryThrowsException_ThrowsUserException()
        {
            // Arrange
            _mockUserReadRepository.Setup(x => x.GetAllAsync())
                .ThrowsAsync(new Exception("Database error"));

            var query = new GetAllUsersQuery();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserException>(() =>
                _handler.Handle(query, CancellationToken.None));

            Assert.Equal("Error al obtener todos los usuarios", exception.Message);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((o, t) => true)), 
                Times.Once);
        }
    }
}
