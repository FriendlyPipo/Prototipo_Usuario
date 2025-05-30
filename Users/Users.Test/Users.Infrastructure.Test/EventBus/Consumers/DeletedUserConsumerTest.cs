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
    public class DeletedUserConsumerTest
    {
        private readonly Mock<IMongoClient> _mockMongoClient;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoCollection<MongoUserDocument>> _mockUserCollection;
        private readonly Mock<IMongoCollection<MongoRoleDocument>> _mockRoleCollection;
        private readonly Mock<IOptions<MongoDBSettings>> _mockSettings;
        private readonly DeletedUserConsumer _consumer;
        private AsyncEventingBasicConsumer? _capturedConsumer;

        public DeletedUserConsumerTest()
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

            _consumer = new DeletedUserConsumer(_mockMongoClient.Object, _mockSettings.Object);
        }

        [Fact]
        public async Task Start_ShouldProcessMessage_AndDeleteDocuments()
        {
            // Arrange
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IChannel>();

            var testEvent = new UserDeletedEvent(Guid.NewGuid());

            mockConnection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockChannel.Object);

            mockChannel.Setup(x => x.QueueDeclareAsync(
                "user.deleted",
                true,
                false,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                false,
                false,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueueDeclareOk("test-queue", 0, 1));

            mockChannel.Setup(m => m.BasicConsumeAsync(
                "user.deleted",
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

            var deleteResult = new DeleteResult.Acknowledged(1);
            _mockUserCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<MongoUserDocument>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteResult);

            _mockRoleCollection.Setup(x => x.DeleteManyAsync(
                It.IsAny<FilterDefinition<MongoRoleDocument>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteResult);

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
            _mockUserCollection.Verify(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<MongoUserDocument>>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _mockRoleCollection.Verify(x => x.DeleteManyAsync(
                It.IsAny<FilterDefinition<MongoRoleDocument>>(),
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
                "user.deleted",
                true,
                false,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                false,
                false,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new QueueDeclareOk("test-queue", 0, 1));

            mockChannel.Setup(m => m.BasicConsumeAsync(
                "user.deleted",
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
            _mockUserCollection.Verify(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<MongoUserDocument>>(),
                It.IsAny<CancellationToken>()),
                Times.Never);

            _mockRoleCollection.Verify(x => x.DeleteManyAsync(
                It.IsAny<FilterDefinition<MongoRoleDocument>>(),
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
