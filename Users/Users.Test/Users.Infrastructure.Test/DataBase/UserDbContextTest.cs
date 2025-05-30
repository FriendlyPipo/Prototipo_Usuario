using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Users.Domain.Entities;
using Users.Infrastructure.Database;
using Xunit;
using System.Linq.Expressions;

namespace Users.Test.Users.Infrastructure.Test.DataBase
{
    public class UserDbContextTest : IDisposable
    {
        private readonly DbContextOptions<UserDbContext> _options;
        private readonly UserDbContext _context;

        public UserDbContextTest()
        {
            _options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: "TestUserDb")
                .ConfigureWarnings(w => 
                {
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning);
                })
                .Options;

            _context = new UserDbContext(_options);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public void Constructor_InitializesDbSets()
        {
            // Assert
            Assert.NotNull(_context.User);
            Assert.NotNull(_context.Role);
        }

        [Fact]
        public void DbContext_ReturnsCorrectInstance()
        {
            // Act
            var dbContext = _context.DbContext;

            // Assert
            Assert.NotNull(dbContext);
            Assert.Same(_context, dbContext);
        }

        [Fact]
        public void BeginTransaction_WithInMemoryDatabase_ReturnsTransactionProxy()
        {
            // Act
            var transaction = _context.BeginTransaction();

            // Assert
            Assert.NotNull(transaction);
            Assert.IsType<UserDbContextTransactionProxy>(transaction);
        }

        [Fact]
        public void SetPropertyIsModifiedToFalse_SetsPropertyModifiedState()
        {
            // Arrange
            var userName = "TestUser";
            var userLastName = $"TestLastName";
            var userEmail = "TestEmail@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "TestDirection";
            var user = new User(userName, userLastName, userEmail, userPhoneNumber, userDirection);
            _context.User.Add(user);
            _context.SaveChanges();

            // Act
            Expression<Func<User, string>> propertyExpression = u => u.UserName;
            _context.SetPropertyIsModifiedToFalse(user, propertyExpression);

            // Assert
            var entry = _context.Entry(user);
            Assert.False(entry.Property(u => u.UserName).IsModified);
        }

        [Fact]
        public void ChangeEntityState_ChangesStateCorrectly()
        {
            // Arrange
            var userName = "TestUser";
            var userLastName = $"TestLastName";
            var userEmail = "TestEmail@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "TestDirection";
            var user = new User(userName, userLastName, userEmail, userPhoneNumber, userDirection);

            // Act
            _context.ChangeEntityState(user, EntityState.Added);

            // Assert
            var entry = _context.Entry(user);
            Assert.Equal(EntityState.Added, entry.State);
        }

        [Fact]
        public void ChangeEntityState_WithNullEntity_DoesNotThrow()
        {
            // Act & Assert
            User? nullUser = null;
            var exception = Record.Exception(() => _context.ChangeEntityState(nullUser, EntityState.Added));
            Assert.Null(exception);
        }

        [Fact]
        public async Task SaveChangesAsync_WithNewEntity_SetsCreatedAndUpdatedDates()
        {
            // Arrange
            var userName = "TestUser";
            var userLastName = $"TestLastName";
            var userEmail = "TestEmail@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "TestDirection";
            var user = new User(userName, userLastName, userEmail, userPhoneNumber, userDirection);
            _context.User.Add(user);

            // Act
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotEqual(DateTime.MinValue, user.CreatedAt);
            Assert.NotEqual(DateTime.MinValue, user.UpdatedAt);
            
            var timeDiff = (user.UpdatedAt - user.CreatedAt) ?? TimeSpan.Zero;
            Assert.True(timeDiff.TotalSeconds < 1, "CreatedAt and UpdatedAt should be set to very close timestamps");
        }

        [Fact]
        public async Task SaveChangesAsync_WithModifiedEntity_UpdatesOnlyUpdatedDate()
        {
            // Arrange
            var userName = "TestUser";
            var userLastName = $"TestLastName";
            var userEmail = "TestEmail@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "TestDirection";
            var user = new User(userName, userLastName, userEmail, userPhoneNumber, userDirection);
            _context.User.Add(user);
            await _context.SaveChangesAsync();
            
            var originalCreatedAt = user.CreatedAt;
            var originalUpdatedAt = user.UpdatedAt;
            await Task.Delay(100);

            user.UpdateUserName("TestUserUpdated");
            _context.Entry(user).State = EntityState.Modified;

            // Act
            await _context.SaveChangesAsync();

            // Assert
            Assert.Equal(originalCreatedAt, user.CreatedAt);
            Assert.NotEqual(originalUpdatedAt, user.UpdatedAt);
        }

        [Fact]
        public async Task SaveChangesAsync_WithUser_SetsUserInformation()
        {
            // Arrange
                        var userName = "TestUser";
            var userLastName = $"TestLastName";
            var userEmail = "TestEmail@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "TestDirection";
            var user = new User(userName, userLastName, userEmail, userPhoneNumber, userDirection);
            _context.User.Add(user);
            var testUser = "TestUser123";

            // Act
            await _context.SaveChangesAsync(testUser);

            // Assert
            Assert.Equal(testUser, user.CreatedBy);
            Assert.NotEqual(default, user.CreatedAt);
            Assert.Null(user.UpdatedBy);
            Assert.Equal(default, user.UpdatedAt);
        }

        [Fact]
        public async Task SaveChangesAsync_WithUserAndModifiedEntity_UpdatesUserInformation()
        {
            // Arrange
            var userName = "TestUser";
            var userLastName = $"TestLastName";
            var userEmail = "TestEmail@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "TestDirection";
            var user = new User(userName, userLastName, userEmail, userPhoneNumber, userDirection);
            _context.User.Add(user);
            await _context.SaveChangesAsync("Creator");

            var originalCreatedBy = user.CreatedBy;
            var originalCreatedAt = user.CreatedAt;

            // Modify the entity
            user.UpdateUserName("Jane");
            _context.Entry(user).State = EntityState.Modified;

            // Act
            await _context.SaveChangesAsync("Modifier");

            // Assert
            Assert.Equal(originalCreatedBy, user.CreatedBy);
            Assert.Equal(originalCreatedAt, user.CreatedAt);
            Assert.Equal("Modifier", user.UpdatedBy);
            Assert.NotEqual(default, user.UpdatedAt);
        }

        [Fact]
        public async Task SaveEfContextChanges_ReturnsTrue_OnSuccess()
        {
            // Arrange
            var userName = "TestUser";
            var userLastName = $"TestLastName";
            var userEmail = "TestEmail@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "TestDirection";
            var user = new User(userName, userLastName, userEmail, userPhoneNumber, userDirection);
            _context.User.Add(user);

            // Act
            var result = await _context.SaveEfContextChanges();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SaveEfContextChanges_WithUser_ReturnsTrue_OnSuccess()
        {
            // Arrange
            var userName = "TestUser";
            var userLastName = $"TestLastName";
            var userEmail = "TestEmail@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "TestDirection";
            var user = new User(userName, userLastName, userEmail, userPhoneNumber, userDirection);
            _context.User.Add(user);

            // Act
            var result = await _context.SaveEfContextChanges("TestUser");

            // Assert
            Assert.True(result);
        }
    }
}
