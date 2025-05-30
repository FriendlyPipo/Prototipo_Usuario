using System;
using Xunit;
using Users.Infrastructure.EventBus.Events;

namespace Users.Test.Users.Infrastructure.Test.EventBus.Events
{
    public class UserUpdatedEventTests
    {
        [Fact]
        public void UserUpdatedEvent_ShouldCreateWithValidParameters()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userName = "Test";
            var userLastName = "User";
            var userEmail = "Test@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "123 Main St";
            var createdAt = DateTime.UtcNow;
            var createdBy = "System";
            var updatedAt = DateTime.UtcNow;
            var updatedBy = "Admin";
            var roleId = Guid.NewGuid();
            var roleName = "Administrador";

            // Act
            var userUpdatedEvent = new UserUpdatedEvent(
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
            Assert.Equal(userId, userUpdatedEvent.UserId);
            Assert.Equal(userName, userUpdatedEvent.UserName);
            Assert.Equal(userLastName, userUpdatedEvent.UserLastName);
            Assert.Equal(userEmail, userUpdatedEvent.UserEmail);
            Assert.Equal(userPhoneNumber, userUpdatedEvent.UserPhoneNumber);
            Assert.Equal(userDirection, userUpdatedEvent.UserDirection);
            Assert.Equal(createdAt, userUpdatedEvent.CreatedAt);
            Assert.Equal(createdBy, userUpdatedEvent.CreatedBy);
            Assert.Equal(updatedAt, userUpdatedEvent.UpdatedAt);
            Assert.Equal(updatedBy, userUpdatedEvent.UpdatedBy);
            Assert.Equal(roleId, userUpdatedEvent.RoleId);
            Assert.Equal(roleName, userUpdatedEvent.RoleName);
        }

        [Fact]
        public void UserUpdatedEvent_ShouldCreateWithNullableParameters()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var roleId = Guid.NewGuid();

            // Act
            var userUpdatedEvent = new UserUpdatedEvent(
                userId,
                null,
                null,
                null,
                null,
                null,
                createdAt,
                null,
                null,
                null,
                roleId,
                null);

            // Assert
            Assert.Equal(userId, userUpdatedEvent.UserId);
            Assert.Null(userUpdatedEvent.UserName);
            Assert.Null(userUpdatedEvent.UserLastName);
            Assert.Null(userUpdatedEvent.UserEmail);
            Assert.Null(userUpdatedEvent.UserPhoneNumber);
            Assert.Null(userUpdatedEvent.UserDirection);
            Assert.Equal(createdAt, userUpdatedEvent.CreatedAt);
            Assert.Null(userUpdatedEvent.CreatedBy);
            Assert.Null(userUpdatedEvent.UpdatedAt);
            Assert.Null(userUpdatedEvent.UpdatedBy);
            Assert.Equal(roleId, userUpdatedEvent.RoleId);
            Assert.Null(userUpdatedEvent.RoleName);
        }

        [Fact]
        public void UserUpdatedEvent_ShouldSupportValueEquality()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var roleId = Guid.NewGuid();

            var event1 = new UserUpdatedEvent(
                userId, "Test", "User", "Test@example.com", "12341567890",
                "123 Main St", createdAt, "System", null, null, roleId, "Postor");

            var event2 = new UserUpdatedEvent(
                userId, "Test", "User", "Test@example.com", "12341567890",
                "123 Main St", createdAt, "System", null, null, roleId, "Postor");

            // Act & Assert
            Assert.Equal(event1, event2);
            Assert.True(event1.Equals(event2));
            Assert.Equal(event1.GetHashCode(), event2.GetHashCode());
        }
    }
} 