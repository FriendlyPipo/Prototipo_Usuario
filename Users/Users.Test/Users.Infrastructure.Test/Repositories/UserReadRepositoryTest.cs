using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Moq;
using Users.Core.Repositories; 
using Users.Domain.Entities;
using Users.Infrastructure.Database;
using Users.Infrastructure.Repositories;
using Users.Infrastructure.Settings; 
using Xunit;


namespace Users.Test.Infrastructure.Repositories
{


    public class UserReadRepositoryTest
    {
        private readonly Mock<MongoDbContext> _mockDbContext;
        private readonly Mock<IMongoCollection<MongoUserDocument>> _mockUsersCollection;
        private readonly Mock<IMongoCollection<MongoRoleDocument>> _mockRolesCollection;
        private readonly UserReadRepository _userReadRepository;

        public UserReadRepositoryTest()
        {
            _mockUsersCollection = new Mock<IMongoCollection<MongoUserDocument>>();
            _mockRolesCollection = new Mock<IMongoCollection<MongoRoleDocument>>();

            var mockMongoSettings = new Mock<Microsoft.Extensions.Options.IOptions<MongoDBSettings>>();
            var settings = new MongoDBSettings { ConnectionString = "mongodb://testhost:27017", DatabaseName = "TestDatabase" };
            mockMongoSettings.Setup(s => s.Value).Returns(settings);

            _mockDbContext = new Mock<MongoDbContext>(mockMongoSettings.Object);
            _mockDbContext.Setup(db => db.User).Returns(_mockUsersCollection.Object);
            _mockDbContext.Setup(db => db.Role).Returns(_mockRolesCollection.Object);

            _userReadRepository = new UserReadRepository(_mockDbContext.Object);
        }

        private Mock<IAsyncCursor<T>> SetupMockCursor<T>(List<T> items)
        {
            var mockCursor = new Mock<IAsyncCursor<T>>();
            mockCursor.Setup(_ => _.Current).Returns(items);

            mockCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(items.Any())
                .Returns(false);
            mockCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(items.Any()))
                .Returns(Task.FromResult(false));
            return mockCursor;
        }

        [Fact]
        public async Task GetByIdAsync_UserExists_RoleExists_ReturnsUserWithRole()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDocument = new MongoUserDocument { UserId = userId, UserEmail = "test@example.com", UserName = "Test User" };
            var roleDocument = new MongoRoleDocument { UserId = userId, RoleName = "Administrador" };

            var mockUserCursor = SetupMockCursor(new List<MongoUserDocument> { userDocument });
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(),
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUserCursor.Object);

            var mockRoleCursor = SetupMockCursor(new List<MongoRoleDocument> { roleDocument });
            _mockRolesCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoRoleDocument>>(),
                        It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRoleCursor.Object);

            // Act
            var result = await _userReadRepository.GetByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("Administrador", result.UserRole);
            _mockUsersCollection.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<MongoUserDocument>>(), It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockRolesCollection.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<MongoRoleDocument>>(), It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_UserExists_RoleNotExists_ReturnsUserWithNullRole()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDocument = new MongoUserDocument { UserId = userId, UserEmail = "test@example.com", UserName = "TestUser" };

            var mockUserCursor = SetupMockCursor(new List<MongoUserDocument> { userDocument });
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(),
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUserCursor.Object);

            var mockEmptyRoleCursor = SetupMockCursor(new List<MongoRoleDocument>()); 
            _mockRolesCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoRoleDocument>>(),
                        It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockEmptyRoleCursor.Object);

            // Act
            var result = await _userReadRepository.GetByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Null(result.UserRole);
        }

        [Fact]
        public async Task GetByIdAsync_UserNotExists_ReturnsNull()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var mockEmptyUserCursor = SetupMockCursor(new List<MongoUserDocument>()); 
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(),
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockEmptyUserCursor.Object);

            // Act
            var result = await _userReadRepository.GetByIdAsync(userId);

            // Assert
            Assert.Null(result);
            _mockRolesCollection.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<MongoRoleDocument>>(), It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        }


        [Fact]
        public async Task GetByEmailAsync_UserExists_RoleExists_ReturnsUserWithRole()
        {
            // Arrange
            var userEmail = "test@example.com";
            var userId = Guid.NewGuid(); 
            var userDocument = new MongoUserDocument { UserId = userId, UserEmail = userEmail, UserName = "Test User" };
            var roleDocument = new MongoRoleDocument { UserId = userId, RoleName = "Soporte" };

            var mockUserCursor = SetupMockCursor(new List<MongoUserDocument> { userDocument });
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(),
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUserCursor.Object);

            var mockRoleCursor = SetupMockCursor(new List<MongoRoleDocument> { roleDocument });
            _mockRolesCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoRoleDocument>>(), 
                        It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRoleCursor.Object);

            // Act
            var result = await _userReadRepository.GetByEmailAsync(userEmail);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userEmail, result.UserEmail);
            Assert.Equal("Soporte", result.UserRole);
        }

        [Fact]
        public async Task GetByEmailAsync_UserExists_RoleNotExists_ReturnsUserWithNullRole()
        {
            // Arrange
            var userEmail = "test@example.com";
            var userId = Guid.NewGuid();
            var userDocument = new MongoUserDocument { UserId = userId, UserEmail = userEmail, UserName = "Test User" };

            var mockUserCursor = SetupMockCursor(new List<MongoUserDocument> { userDocument });
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(),
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUserCursor.Object);

            var mockEmptyRoleCursor = SetupMockCursor(new List<MongoRoleDocument>());
            _mockRolesCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoRoleDocument>>(),
                        It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockEmptyRoleCursor.Object);

            // Act
            var result = await _userReadRepository.GetByEmailAsync(userEmail);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userEmail, result.UserEmail);
            Assert.Null(result.UserRole);
        }

        [Fact]
        public async Task GetByEmailAsync_UserNotExists_ReturnsNull()
        {
            // Arrange
            var userEmail = "nonexistent@example.com";

            var mockEmptyUserCursor = SetupMockCursor(new List<MongoUserDocument>());
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(),
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockEmptyUserCursor.Object);

            // Act
            var result = await _userReadRepository.GetByEmailAsync(userEmail);

            // Assert
            Assert.Null(result);
            _mockRolesCollection.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<MongoRoleDocument>>(), It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_MultipleUsers_VariedRoles_ReturnsAllUsersWithCorrectRoles()
        {
            // Arrange
            var user1Id = Guid.NewGuid();
            var user2Id = Guid.NewGuid();
            var user3Id = Guid.NewGuid();

            var users = new List<MongoUserDocument>
            {
                new MongoUserDocument { UserId = user1Id, UserName = "User One" },
                new MongoUserDocument { UserId = user2Id, UserName = "User Two" }, 
                new MongoUserDocument { UserId = user3Id, UserName = "User Three" }
            };

            var roles = new List<MongoRoleDocument>
            {
                new MongoRoleDocument { UserId = user1Id, RoleName = "Administrador" },
                new MongoRoleDocument { UserId = user3Id, RoleName = "Soporte" }
            };

            var mockUsersCursor = SetupMockCursor(users);
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(), 
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUsersCursor.Object);

            var mockRolesCursor = SetupMockCursor(roles);
            _mockRolesCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoRoleDocument>>(),
                        It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRolesCursor.Object);

            // Act
            var result = (await _userReadRepository.GetAllAsync()).ToList();

            // Assert
            Assert.Equal(3, result.Count);

            var user1Result = result.FirstOrDefault(u => u.UserId == user1Id);
            Assert.NotNull(user1Result);
            Assert.Equal("Administrador", user1Result.UserRole);

            var user2Result = result.FirstOrDefault(u => u.UserId == user2Id);
            Assert.NotNull(user2Result);
            Assert.Null(user2Result.UserRole); 

            var user3Result = result.FirstOrDefault(u => u.UserId == user3Id);
            Assert.NotNull(user3Result);
            Assert.Equal("Soporte", user3Result.UserRole);

            _mockUsersCollection.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<MongoUserDocument>>(), It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockRolesCollection.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<MongoRoleDocument>>(), It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_NoUsers_ReturnsEmptyList()
        {
            // Arrange
            var mockEmptyUsersCursor = SetupMockCursor(new List<MongoUserDocument>());
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(),
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockEmptyUsersCursor.Object);

            // Act
            var result = (await _userReadRepository.GetAllAsync()).ToList();

            // Assert
            Assert.Empty(result);
            _mockRolesCollection.Verify(c => c.FindAsync(It.IsAny<FilterDefinition<MongoRoleDocument>>(), It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAllAsync_UsersExist_NoRolesInCollection_ReturnsUsersWithNullRoles()
        {
            // Arrange
            var user1Id = Guid.NewGuid();
            var users = new List<MongoUserDocument>
            {
                new MongoUserDocument { UserId = user1Id, UserName = "User One" }
            };

            var mockUsersCursor = SetupMockCursor(users);
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(),
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUsersCursor.Object);

            var mockEmptyRolesCursor = SetupMockCursor(new List<MongoRoleDocument>()); 
            _mockRolesCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoRoleDocument>>(),
                        It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockEmptyRolesCursor.Object);

            // Act
            var result = (await _userReadRepository.GetAllAsync()).ToList();

            // Assert
            Assert.Single(result);
            Assert.Null(result.First().UserRole);
        }

        [Fact]
        public async Task GetAllAsync_UsersExist_RolesForOtherUsersExist_ReturnsUsersWithCorrectlyMappedOrNullRoles()
        {
            // Arrange
            var user1Id = Guid.NewGuid(); 
            var user2Id = Guid.NewGuid(); 
            var otherUserId = Guid.NewGuid(); 

            var usersToFetch = new List<MongoUserDocument>
            {
                new MongoUserDocument { UserId = user1Id, UserName = "TestUser 1" },
                new MongoUserDocument { UserId = user2Id, UserName = "TestUser 2" }
            };

            var rolesInDb = new List<MongoRoleDocument>
            {
                new MongoRoleDocument { UserId = user2Id, RoleName = "Soporte" },
                new MongoRoleDocument { UserId = otherUserId, RoleName = "Administrador" }
            };

            var mockUsersCursor = SetupMockCursor(usersToFetch);
            _mockUsersCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoUserDocument>>(),
                        It.IsAny<FindOptions<MongoUserDocument, MongoUserDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUsersCursor.Object);

            var mockRolesCursor = SetupMockCursor(rolesInDb);
            _mockRolesCollection.Setup(c => c.FindAsync(
                        It.IsAny<FilterDefinition<MongoRoleDocument>>(),
                        It.IsAny<FindOptions<MongoRoleDocument, MongoRoleDocument>>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRolesCursor.Object);

            // Act
            var result = (await _userReadRepository.GetAllAsync()).ToList();

            // Assert
            Assert.Equal(2, result.Count);

            var user1Result = result.FirstOrDefault(u => u.UserId == user1Id);
            Assert.NotNull(user1Result);
            Assert.Null(user1Result.UserRole);

            var user2Result = result.FirstOrDefault(u => u.UserId == user2Id);
            Assert.NotNull(user2Result);
            Assert.Equal("Soporte", user2Result.UserRole);
        }
    }
}