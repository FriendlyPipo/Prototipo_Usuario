using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Users.Domain.Entities;
using Users.Infrastructure.EventBus.Consumers;
using Users.Infrastructure.EventBus.Events;
using Users.Infrastructure.Settings;
using Users.Core.Events;
using Xunit;

namespace Users.Test.Users.Infrastructure.Test.EventBus.Consumers
{
    public class UpdatedUserConsumerTest
    {
        private readonly Mock<IMongoClient> _mockMongoClient;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoCollection<MongoUserDocument>> _mockUserCollection;
        private readonly Mock<IMongoCollection<MongoRoleDocument>> _mockRoleCollection;
        private readonly Mock<IOptions<MongoDBSettings>> _mockSettings;
        private readonly UpdatedUserConsumer _consumer;
        private AsyncEventingBasicConsumer? _capturedConsumer;

        public UpdatedUserConsumerTest()
        {
            _mockMongoClient = new Mock<IMongoClient>();
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockUserCollection = new Mock<IMongoCollection<MongoUserDocument>>();
            _mockRoleCollection = new Mock<IMongoCollection<MongoRoleDocument>>();
            _mockSettings = new Mock<IOptions<MongoDBSettings>>();

            _mockSettings.Setup(x => x.Value).Returns(new MongoDBSettings { DatabaseName = "TestDb" });
            _mockMongoClient.Setup(x => x.GetDatabase(It.IsAny<string>(), null)).Returns(_mockDatabase.Object);
            _mockDatabase.Setup(x => x.GetCollection<MongoUserDocument>("User", null)).Returns(_mockUserCollection.Object);
            _mockDatabase.Setup(x => x.GetCollection<MongoRoleDocument>("Role", null)).Returns(_mockRoleCollection.Object);

            _consumer = new UpdatedUserConsumer(_mockMongoClient.Object, _mockSettings.Object);
        }

        [Fact]
        public async Task Start_ShouldProcessMessage_AndUpdateDocuments()
        {
            // Arrange
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IChannel>();

            var testEvent = new UserUpdatedEvent(
                Guid.NewGuid(),
                "Updated Test",
                "Updated User",
                "updated@example.com",
                "98765432109",
                "Updated Address",
                DateTime.UtcNow,
                "System",
                DateTime.UtcNow,
                "Postor",
                Guid.NewGuid(),
                "TestRole"
            );

            mockConnection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockChannel.Object);

            mockChannel.Setup(x => x.QueueDeclareAsync(
                "user.updated",
                true,
                false,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                false,
                false,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueueDeclareOk("test-queue", 0, 1));

            mockChannel.Setup(m => m.BasicConsumeAsync(
                "user.updated",
                false,
                It.IsAny<string>(),
                false,
                false,
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<IAsyncBasicConsumer>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IAsyncBasicConsumer, CancellationToken>(
                (queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer, token) =>
                {
                    _capturedConsumer = consumer as AsyncEventingBasicConsumer;
                })
            .ReturnsAsync("test-consumer-tag");

            var updateResult = new UpdateResult.Acknowledged(1, 1, null);
            _mockUserCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<MongoUserDocument>>(),
                It.IsAny<UpdateDefinition<MongoUserDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            _mockRoleCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<MongoRoleDocument>>(),
                It.IsAny<UpdateDefinition<MongoRoleDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            await _consumer.Start(mockConnection.Object);
            Assert.NotNull(_capturedConsumer);

            var json = JsonSerializer.Serialize(testEvent);
            var body = Encoding.UTF8.GetBytes(json);
            var deliveryArgs = new BasicDeliverEventArgs(
                "consumer-tag",
                1,
                false,
                "exchange",
                "routing-key",
                null,
                new ReadOnlyMemory<byte>(body));

            await _capturedConsumer.HandleBasicDeliverAsync(
                deliveryArgs.ConsumerTag,
                deliveryArgs.DeliveryTag,
                deliveryArgs.Redelivered,
                deliveryArgs.Exchange,
                deliveryArgs.RoutingKey,
                deliveryArgs.BasicProperties,
                deliveryArgs.Body);

            // Assert
            _mockUserCollection.Verify(x => x.UpdateOneAsync(
                It.Is<FilterDefinition<MongoUserDocument>>(f => true),
                It.Is<UpdateDefinition<MongoUserDocument>>(u => true),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _mockRoleCollection.Verify(x => x.UpdateOneAsync(
                It.Is<FilterDefinition<MongoRoleDocument>>(f => true),
                It.Is<UpdateDefinition<MongoRoleDocument>>(u => true),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            mockChannel.Verify(x => x.BasicAckAsync(
                It.Is<ulong>(tag => tag == deliveryArgs.DeliveryTag),
                false,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Start_ShouldHandleDeserializationError()
        {
            // Arrange
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IChannel>();

            mockConnection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockChannel.Object);

            mockChannel.Setup(x => x.QueueDeclareAsync(
                "user.updated",
                true,
                false,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                false,
                false,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueueDeclareOk("test-queue", 0, 1));

            mockChannel.Setup(m => m.BasicConsumeAsync(
                "user.updated",
                false,
                It.IsAny<string>(),
                false,
                false,
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<IAsyncBasicConsumer>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object>, IAsyncBasicConsumer, CancellationToken>(
                (queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer, token) =>
                {
                    _capturedConsumer = consumer as AsyncEventingBasicConsumer;
                })
            .ReturnsAsync("test-consumer-tag");

            // Act
            await _consumer.Start(mockConnection.Object);
            Assert.NotNull(_capturedConsumer);

            var invalidJson = "invalid json";
            var body = Encoding.UTF8.GetBytes(invalidJson);
            var deliveryArgs = new BasicDeliverEventArgs(
                "consumer-tag",
                1,
                false,
                "exchange",
                "routing-key",
                null,
                new ReadOnlyMemory<byte>(body));

            await _capturedConsumer.HandleBasicDeliverAsync(
                deliveryArgs.ConsumerTag,
                deliveryArgs.DeliveryTag,
                deliveryArgs.Redelivered,
                deliveryArgs.Exchange,
                deliveryArgs.RoutingKey,
                deliveryArgs.BasicProperties,
                deliveryArgs.Body);

            // Assert
            _mockUserCollection.Verify(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<MongoUserDocument>>(),
                It.IsAny<UpdateDefinition<MongoUserDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Never);

            _mockRoleCollection.Verify(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<MongoRoleDocument>>(),
                It.IsAny<UpdateDefinition<MongoRoleDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Never);

            mockChannel.Verify(x => x.BasicAckAsync(
                It.Is<ulong>(tag => tag == deliveryArgs.DeliveryTag),
                false,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
