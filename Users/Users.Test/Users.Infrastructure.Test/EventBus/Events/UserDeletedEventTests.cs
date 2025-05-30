using System;
using Xunit;
using Users.Infrastructure.EventBus.Events;

namespace Users.Test.Users.Infrastructure.Test.EventBus.Events
{
    public class UserDeletedEventTests
    {
        [Fact]
        public void UserDeletedEvent_ShouldCreateWithValidUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var userDeletedEvent = new UserDeletedEvent(userId);

            // Assert
            Assert.Equal(userId, userDeletedEvent.UserId);
        }

        [Fact]
        public void UserDeletedEvent_ShouldSupportValueEquality()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var event1 = new UserDeletedEvent(userId);
            var event2 = new UserDeletedEvent(userId);

            // Act & Assert
            Assert.Equal(event1, event2);
            Assert.True(event1.Equals(event2));
            Assert.Equal(event1.GetHashCode(), event2.GetHashCode());
        }

        [Fact]
        public void UserDeletedEvent_ShouldNotBeEqualWithDifferentUserId()
        {
            // Arrange
            var event1 = new UserDeletedEvent(Guid.NewGuid());
            var event2 = new UserDeletedEvent(Guid.NewGuid());

            // Act & Assert
            Assert.NotEqual(event1, event2);
            Assert.False(event1.Equals(event2));
            Assert.NotEqual(event1.GetHashCode(), event2.GetHashCode());
        }
    }
} 