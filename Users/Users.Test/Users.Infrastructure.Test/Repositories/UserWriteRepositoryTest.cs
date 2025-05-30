using Xunit;
using Moq;
using Moq.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Infrastructure.Repositories;
using Users.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading; 
using System.Threading.Tasks;

namespace Users.Test.Infrastructure.Repositories
{
    public class UserWriteRepositoryTest
    {
        private Mock<UserDbContext> _mockDbContext;
        private Mock<DbSet<User>> _mockUserDbSet;
        private UserWriteRepository _userWriteRepository;

        public UserWriteRepositoryTest()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _mockDbContext = new Mock<UserDbContext>(options);
            _mockUserDbSet = new Mock<DbSet<User>>();
            _mockDbContext.Setup(x => x.User).ReturnsDbSet(new List<User>(), _mockUserDbSet);

            _userWriteRepository = new UserWriteRepository(_mockDbContext.Object);
        }

        private User TestUser()
        {
            var userName = "TestUser";
            var userLastName = $"TestLastName";
            var userEmail = "TestEmail@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "TestDirection";

            var user = new User(
                userName,
                userLastName,
                userEmail,
                userPhoneNumber,
                userDirection
            );
            return user;
        }

        [Fact]
        public async Task CreateAsync_ShouldAddUser()
        {
            // Arrange
            var user = TestUser();

            // Act
            await _userWriteRepository.CreateAsync(user);

            // Assert
            _mockUserDbSet.Verify(set => set.Add(user), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveUser_WhenUserExists()
        {
            // Arrange
            var userToDelete = TestUser(); 
            var userId = userToDelete.UserId;
            
            var usersList = new List<User> { userToDelete }; 
            _mockDbContext.Setup(x => x.User).ReturnsDbSet(usersList, _mockUserDbSet);

            _mockUserDbSet.Setup(s => s.FindAsync(new object[] { userId }))
                        .ReturnsAsync(userToDelete); 

            // Act
            await _userWriteRepository.DeleteAsync(userId);

            // Assert
            _mockUserDbSet.Verify(set => set.Remove(userToDelete), Times.Once); 
        }

        [Fact]
        public async Task DeleteAsync_ShouldNotRemoveUser_WhenUserDoesNotExist()
        {
            //Arrange
            var nonExistentUserId = Guid.NewGuid();
            var usersList = new List<User>();
            _mockDbContext.Setup(x => x.User).ReturnsDbSet(usersList, _mockUserDbSet);

            // Act
            await _userWriteRepository.DeleteAsync(nonExistentUserId);

            // Assert
            _mockUserDbSet.Verify(set => set.Remove(It.IsAny<User>()), Times.Never);
        }


        [Fact]
        public async Task UpdateAsync_ShouldUpdateUser()
        {
            //Arrange
            var user = TestUser();

            //Act
            await _userWriteRepository.UpdateAsync(user);

            //Assert
            _mockUserDbSet.Verify(set => set.Update(user), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser_WithRoles_WhenUserExists()
        {
            // Arrange
            var user = TestUser();
            var userId = user.UserId;

            var usersList = new List<User> { user };

            _mockDbContext.Setup(x => x.User).ReturnsDbSet(usersList, _mockUserDbSet);

            // Act
            var result = await _userWriteRepository.GetByIdAsync(userId); 

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.UserId, result.UserId);
            Assert.Equal(user.UserName, result.UserName);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var usersList = new List<User>(); 

            _mockDbContext.Setup(x => x.User).ReturnsDbSet(usersList, _mockUserDbSet);

            // Act
            var result = await _userWriteRepository.GetByIdAsync(userId);

            // Assert
            Assert.Null(result);
        }
    }
}
