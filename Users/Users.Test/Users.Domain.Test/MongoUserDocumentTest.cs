using Xunit;
using Users.Domain.Entities;
using System;

namespace Users.Test.Domain
{
    public class MongoUserDocumentTest
    {
        
        [Fact]
        public void MongoUserDocument_Should_SetAndGetProperties_Correctly()
        {
            // Arrange
            var user = new MongoUserDocument();
            var userId = Guid.NewGuid();
            var userName = "UserNameTest";
            var userLastName = "UserLastNameTest";
            var userEmail = "UserTest@example.com";
            var userPhoneNumber = "12345678901";
            var userDirection = "test";
            var createdAt = DateTime.UtcNow;
            var createdBy = "Admin";
            var updatedAt = DateTime.UtcNow.AddHours(1);
            var updatedBy = "Editor";
            var userRole = "Administrador";

            // Act
            user.UserId = userId;
            user.UserName = userName;
            user.UserLastName = userLastName;
            user.UserEmail = userEmail;
            user.UserPhoneNumber = userPhoneNumber;
            user.UserDirection = userDirection;
            user.CreatedAt = createdAt;
            user.CreatedBy = createdBy;
            user.UpdatedAt = updatedAt;
            user.UpdatedBy = updatedBy;
            user.UserRole = userRole;

            // Assert
            Assert.Equal(userId, user.UserId);
            Assert.Equal(userName, user.UserName);
            Assert.Equal(userLastName, user.UserLastName);
            Assert.Equal(userEmail, user.UserEmail);
            Assert.Equal(userPhoneNumber, user.UserPhoneNumber);
            Assert.Equal(userDirection, user.UserDirection);
            Assert.Equal(createdAt, user.CreatedAt);
            Assert.Equal(createdBy, user.CreatedBy);
            Assert.Equal(updatedAt, user.UpdatedAt);
            Assert.Equal(updatedBy, user.UpdatedBy);
            Assert.Equal(userRole, user.UserRole);
        }

    }
}