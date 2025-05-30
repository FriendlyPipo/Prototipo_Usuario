using System;
using Xunit;
using Users.Infrastructure.EventBus.Events;

namespace Users.Test.Users.Infrastructure.Test.EventBus.Events
{
    public class UserCreatedEventTests
    {
        [Fact]
        public void UserCreatedEvent_ShouldCreateWithValidParameters()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userName = "User";
            var userLastName = "Test";
            var userEmail = "Test@example.com";
            var userPhoneNumber = "12345678910";
            var userDirection = "123 Main St";
            var createdAt = DateTime.UtcNow;
            var createdBy = "System";
            var updatedAt = DateTime.UtcNow;
            var updatedBy = "Admin";
            var roleId = Guid.NewGuid();
            var roleName = "Postor";

            // Act
            var userCreatedEvent = new UserCreatedEvent(
                userId,
                userName,
                userLastName,
                userEmail,
                userPhoneNumber,
                userDirection,
                createdAt,
                createdBy,
                updatedAt,
                updatedBy,
                roleId,
                roleName);

            // Assert
            Assert.Equal(userId, userCreatedEvent.UserId);
            Assert.Equal(userName, userCreatedEvent.UserName);
            Assert.Equal(userLastName, userCreatedEvent.UserLastName);
            Assert.Equal(userEmail, userCreatedEvent.UserEmail);
            Assert.Equal(userPhoneNumber, userCreatedEvent.UserPhoneNumber);
            Assert.Equal(userDirection, userCreatedEvent.UserDirection);
            Assert.Equal(createdAt, userCreatedEvent.CreatedAt);
            Assert.Equal(createdBy, userCreatedEvent.CreatedBy);
            Assert.Equal(updatedAt, userCreatedEvent.UpdatedAt);
            Assert.Equal(updatedBy, userCreatedEvent.UpdatedBy);
            Assert.Equal(roleId, userCreatedEvent.RoleId);
            Assert.Equal(roleName, userCreatedEvent.RoleName);
        }

        [Fact]
        public void UserCreatedEvent_ShouldSupportValueEquality()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var roleId = Guid.NewGuid();

            var event1 = new UserCreatedEvent(
                userId, "User", "Test", "Test@example.com", "11234567890",
                "123 Main St", createdAt, "System", null, null, roleId, "Postor");

            var event2 = new UserCreatedEvent(
                userId, "User", "Test", "Test@example.com", "11234567890",
                "123 Main St", createdAt, "System", null, null, roleId, "Postor");

            // Act & Assert
            Assert.Equal(event1, event2);
            Assert.True(event1.Equals(event2));
            Assert.Equal(event1.GetHashCode(), event2.GetHashCode());
        }

        [Fact]
        public void UserCreatedEvent_ShouldNotBeEqualWithDifferentValues()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var event1 = new UserCreatedEvent(
                Guid.NewGuid(), "User", "Test", "Test@example.com", "11234567890",
                "123 Main St", createdAt, "System", null, null, Guid.NewGuid(), "Postor");

            var event2 = new UserCreatedEvent(
                Guid.NewGuid(), "User2", "Test2", "Test2@example.com", "01987654321",
                "456 Oak St", createdAt, "System", null, null, Guid.NewGuid(), "Administrador");

            // Act & Assert
            Assert.NotEqual(event1, event2);
            Assert.False(event1.Equals(event2));
            Assert.NotEqual(event1.GetHashCode(), event2.GetHashCode());
        }
    }
} 