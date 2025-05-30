using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Users.Core.Events;
using Users.Infrastructure.EventBus;
using Users.Infrastructure.EventBus.Events;
using Xunit;

namespace Users.Test.Users.Infrastructure.Test.EventBus
{
    public interface ITestBasicProperties : IBasicProperties, IAmqpHeader { }

    public class TestQueueDeclareOk : QueueDeclareOk
    {
        public TestQueueDeclareOk(string queueName, uint messageCount = 0, uint consumerCount = 0)
            : base(queueName, messageCount, consumerCount)
        {
        }
    }

    public class RabbitMQPublisherTest
    {
        private readonly Mock<IRabbitMQChannelFactory> _mockChannelFactory;
        private readonly Mock<IChannel> _mockChannel;
        private readonly Mock<ILogger<RabbitMQPublisher>> _mockLogger;
        private readonly RabbitMQPublisher _publisher;
        private readonly Mock<ITestBasicProperties> _mockBasicProperties;

        public RabbitMQPublisherTest()
        {
            _mockChannelFactory = new Mock<IRabbitMQChannelFactory>();
            _mockChannel = new Mock<IChannel>();
            _mockLogger = new Mock<ILogger<RabbitMQPublisher>>();
            _mockBasicProperties = new Mock<ITestBasicProperties>();

            _mockChannelFactory
                .Setup(x => x.CreateChannelAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockChannel.Object);

            _mockBasicProperties
                .SetupGet(x => x.Persistent)
                .Returns(true);

            _publisher = new RabbitMQPublisher(_mockChannelFactory.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Publish_ShouldDeclareQueueAndPublishMessage()
        {
            // Arrange
            var message = new UserCreatedEvent(
                Guid.NewGuid(),
                "Test",
                "User",
                "test@example.com",
                "12345678901",
                "Test Address",
                DateTime.UtcNow,
                "System",
                null,
                null,
                Guid.NewGuid(),
                "TestRole"
            );
            var queueName = "test_queue";
            var messageJson = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(messageJson);

            var queueDeclareOk = new TestQueueDeclareOk(queueName);
            _mockChannel
                .Setup(x => x.QueueDeclareAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(queueDeclareOk);

            _mockChannel
                .Setup(x => x.BasicPublishAsync<BasicProperties>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            // Act
            await _publisher.Publish(message, queueName);

            // Assert
            _mockChannelFactory.Verify(x => x.CreateChannelAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockChannel.Verify(x => x.QueueDeclareAsync(
                queueName,
                true,
                false,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                false,
                false,
                It.IsAny<CancellationToken>()), Times.Once);
            _mockChannel.Verify(x => x.BasicPublishAsync<BasicProperties>(
                "",
                queueName,
                false,
                It.IsAny<BasicProperties>(),
                It.Is<ReadOnlyMemory<byte>>(b => b.ToArray().SequenceEqual(body)),
                It.IsAny<CancellationToken>()), Times.Once);
            _mockChannel.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task Publish_ShouldHandleNullMessage()
        {
            // Arrange
            UserCreatedEvent? message = null;
            var queueName = "test_queue";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _publisher.Publish<UserCreatedEvent>(message!, queueName));
        }

        [Fact]
        public async Task Publish_ShouldHandleNullQueueName()
        {
            // Arrange
            var message = new UserCreatedEvent(
                Guid.NewGuid(),
                "Test",
                "User",
                "test@example.com",
                "12345678901",
                "Test Address",
                DateTime.UtcNow,
                "System",
                null,
                null,
                Guid.NewGuid(),
                "TestRole"
            );

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _publisher.Publish(message, null!));
        }

        [Fact]
        public async Task Publish_ShouldHandleChannelFactoryException()
        {
            // Arrange
            var message = new UserCreatedEvent(
                Guid.NewGuid(),
                "Test",
                "User",
                "test@example.com",
                "12345678901",
                "Test Address",
                DateTime.UtcNow,
                "System",
                null,
                null,
                Guid.NewGuid(),
                "TestRole"
            );
            var queueName = "test_queue";
            var expectedException = new Exception("Channel creation failed");

            _mockChannelFactory
                .Setup(x => x.CreateChannelAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _publisher.Publish(message, queueName));

            Assert.Same(expectedException, exception);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.Is<Exception>(ex => ex == expectedException),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task Publish_ShouldHandleQueueDeclareException()
        {
            // Arrange
            var message = new UserCreatedEvent(
                Guid.NewGuid(),
                "Test",
                "User",
                "test@example.com",
                "12345678901",
                "Test Address",
                DateTime.UtcNow,
                "System",
                null,
                null,
                Guid.NewGuid(),
                "TestRole"
            );
            var queueName = "test_queue";
            var expectedException = new Exception("Queue declare failed");

            _mockChannel
                .Setup(x => x.QueueDeclareAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _publisher.Publish(message, queueName));

            Assert.Same(expectedException, exception);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.Is<Exception>(ex => ex == expectedException),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
            _mockChannel.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task Publish_ShouldHandleBasicPublishException()
        {
            // Arrange
            var message = new UserCreatedEvent(
                Guid.NewGuid(),
                "Test",
                "User",
                "test@example.com",
                "12345678901",
                "Test Address",
                DateTime.UtcNow,
                "System",
                null,
                null,
                Guid.NewGuid(),
                "TestRole"
            );
            var queueName = "test_queue";
            var expectedException = new Exception("Basic publish failed");

            var queueDeclareOk = new TestQueueDeclareOk(queueName);
            _mockChannel
                .Setup(x => x.QueueDeclareAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<IDictionary<string, object?>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(queueDeclareOk);

            _mockChannel
                .Setup(x => x.BasicPublishAsync<BasicProperties>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<CancellationToken>()))
                .Throws(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _publisher.Publish(message, queueName));

            Assert.Same(expectedException, exception);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.Is<Exception>(ex => ex == expectedException),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
            _mockChannel.Verify(x => x.Dispose(), Times.Once);
        }
    }
}
