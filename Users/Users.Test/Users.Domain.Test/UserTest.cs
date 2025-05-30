
using Xunit;
using Users.Domain.Entities;
using System;

namespace Users.Test.Domain
{
    public class UserTest
    {
        private const string InitialUserName = "InitialUserTest";
        private const string InitialUserLastName = "InitialLastNameTest";
        private const string InitialUserEmail = "initial@example.com";
        private const string InitialUserPhoneNumber = "04243213211";
        private const string InitialUserDirection = "InitialAddressTest";

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializePropertiesCorrectly()
        {
            //Arrange
            var userName = "testUser";
            var userLastName = "testLastName";
            var userEmail = "test@example.com";
            var userPhoneNumber = "04241231231";
            var userDirection = "testDirection";

            //Act
            var user = new User(
                userName,
                userLastName,
                userEmail,
                userPhoneNumber,
                userDirection);

            //Assert
            Assert.NotEqual(Guid.Empty, user.UserId);
            Assert.Equal(userName, user.UserName);
            Assert.Equal(userLastName, user.UserLastName);
            Assert.Equal(userEmail, user.UserEmail);
            Assert.Equal(userPhoneNumber, user.UserPhoneNumber);
            Assert.Equal(userDirection, user.UserDirection);
        }

        [Fact]
        public void UpdateUserName_WhenCalled_ShouldUpdateUserNameProperty()
        {
            // Arrange
            var user = new User(
                InitialUserName,
                InitialUserLastName,
                InitialUserEmail,
                InitialUserPhoneNumber,
                InitialUserDirection);

            var newName = "testNewName";

            // Act
            user.UpdateUserName(newName);

            // Assert
            Assert.Equal(newName, user.UserName);
        }

        [Fact]
        public void UpdateUserLastName_WhenCalled_ShouldUpdateUserLastNameProperty()
        {
            // Arrange
            var user = new User(
                InitialUserName,
                InitialUserLastName,
                InitialUserEmail,
                InitialUserPhoneNumber,
                InitialUserDirection);

            var newLastName = "testNewLastName";

            // Act
            user.UpdateUserLastName(newLastName);

            // Assert
            Assert.Equal(newLastName, user.UserLastName);
        }

        [Fact]
        public void UpdateUserEmail_WhenCalled_ShouldUpdateUserEmailProperty()
        {
            // Arrange
            var user = new User(
                InitialUserName,
                InitialUserLastName,
                InitialUserEmail,
                InitialUserPhoneNumber,
                InitialUserDirection);
            var newEmail = "testNewEmail@example.com";

            // Act
            user.UpdateUserEmail(newEmail);

            // Assert
            Assert.Equal(newEmail, user.UserEmail);
        }

        [Fact]
        public void UpdateUserPhoneNumber_WhenCalled_ShouldUpdateUserPhoneNumberProperty()
        {
            // Arrange
            var user = new User(
                InitialUserName,
                InitialUserLastName,
                InitialUserEmail,
                InitialUserPhoneNumber,
                InitialUserDirection);
            var newPhoneNumber = "04242392392";

            // Act
            user.UpdateUserPhoneNumber(newPhoneNumber);

            // Assert
            Assert.Equal(newPhoneNumber, user.UserPhoneNumber);
        }

        [Fact]
        public void UpdateUserDirection_WhenCalled_ShouldUpdateUserDirectionProperty()
        {
            // Arrange
            var user = new User(
                InitialUserName,
                InitialUserLastName,
                InitialUserEmail,
                InitialUserPhoneNumber,
                InitialUserDirection);
            var newDirection = "testNewDirection";

            // Act
            user.UpdateUserDirection(newDirection);

            // Assert
            Assert.Equal(newDirection, user.UserDirection);
        }
    }
}