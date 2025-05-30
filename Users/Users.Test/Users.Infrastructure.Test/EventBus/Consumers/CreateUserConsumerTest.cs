using System;
using System.Collections.Generic;
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
using Xunit;

public class CreateUserConsumerTests
{
    private readonly Mock<IMongoClient> _mockMongoClient;
    private readonly Mock<IOptions<MongoDBSettings>> _mockOptions;
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<MongoUserDocument>> _mockUserCollection;
    private readonly Mock<IMongoCollection<MongoRoleDocument>> _mockRoleCollection;
    private readonly Mock<IConnection> _mockConnection;
    private readonly Mock<IChannel> _mockModel;

    private AsyncEventingBasicConsumer? _capturedConsumer;
    private readonly CreateUserConsumer _sut;

    public CreateUserConsumerTests()
    {
        _mockOptions = new Mock<IOptions<MongoDBSettings>>();
        _mockMongoClient = new Mock<IMongoClient>();
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockUserCollection = new Mock<IMongoCollection<MongoUserDocument>>();
        _mockRoleCollection = new Mock<IMongoCollection<MongoRoleDocument>>();
        _mockConnection = new Mock<IConnection>();
        _mockModel = new Mock<IChannel>();

        var mongoSettings = new MongoDBSettings { DatabaseName = "TestDb" };
        _mockOptions.Setup(o => o.Value).Returns(mongoSettings);

        _mockMongoClient.Setup(c => c.GetDatabase(mongoSettings.DatabaseName, null))
                      .Returns(_mockDatabase.Object);

        _mockDatabase.Setup(db => db.GetCollection<MongoUserDocument>("User", null))
                   .Returns(_mockUserCollection.Object);
        _mockDatabase.Setup(db => db.GetCollection<MongoRoleDocument>("Role", null))
                   .Returns(_mockRoleCollection.Object);

        _mockUserCollection.Setup(c => c.InsertOneAsync(It.IsAny<MongoUserDocument>(), new InsertOneOptions(), It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);
        _mockRoleCollection.Setup(c => c.InsertOneAsync(It.IsAny<MongoRoleDocument>(), new InsertOneOptions(), It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);

        _mockConnection.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(_mockModel.Object);

        _mockModel.Setup(m => m.QueueDeclareAsync(
            "user.created",
            true,
            false,
            false,
            It.IsAny<IDictionary<string, object?>>(),
            false,
            false,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new QueueDeclareOk("user.created", 0, 0));

        _mockModel.Setup(m => m.BasicConsumeAsync(
            "user.created",
            true,
            It.IsAny<string>(),
            false,
            false,
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<IAsyncBasicConsumer>(),
            It.IsAny<CancellationToken>()))
         .Callback<string, bool, string, bool, bool, IDictionary<string, object?>, IAsyncBasicConsumer, CancellationToken>(
            (queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer, token) =>
            {
                _capturedConsumer = consumer as AsyncEventingBasicConsumer;
            })
         .ReturnsAsync("test-consumer-tag");

        _sut = new CreateUserConsumer(_mockMongoClient.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task Start_WhenValidUserCreatedEventReceived_ShouldInsertUserAndRoleDocuments()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var userCreatedEvent = new UserCreatedEvent(
            UserId: userId,
            UserName: "TestUser",
            UserLastName: "TestLastName",
            UserEmail: "test@example.com",
            UserPhoneNumber: "1234567890",
            UserDirection: "123 Test St",
            CreatedAt: DateTime.UtcNow,
            CreatedBy: "UnitTest",
            UpdatedAt: DateTime.UtcNow,
            UpdatedBy: "UnitTest",
            RoleId: roleId,
            RoleName: "TestRole"
        );
        var jsonPayload = JsonSerializer.Serialize(userCreatedEvent);

        // Act
        await _sut.Start(_mockConnection.Object);
        Assert.NotNull(_capturedConsumer);

        var deliveryArgs = new BasicDeliverEventArgs(
            "test-consumer-tag",
            1UL,
            false,
            string.Empty,
            "user.created",
            null,
            new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(jsonPayload)));

        await _capturedConsumer.HandleBasicDeliverAsync(
            deliveryArgs.ConsumerTag,
            deliveryArgs.DeliveryTag,
            deliveryArgs.Redelivered,
            deliveryArgs.Exchange,
            deliveryArgs.RoutingKey,
            deliveryArgs.BasicProperties,
            deliveryArgs.Body
        );

        // Assert
        _mockUserCollection.Verify(
            c => c.InsertOneAsync(
                It.Is<MongoUserDocument>(doc =>
                    doc.UserId == userCreatedEvent.UserId &&
                    doc.UserName == userCreatedEvent.UserName &&
                    doc.UserLastName == userCreatedEvent.UserLastName &&
                    doc.UserEmail == userCreatedEvent.UserEmail &&
                    doc.UserPhoneNumber == userCreatedEvent.UserPhoneNumber &&
                    doc.UserDirection == userCreatedEvent.UserDirection &&
                    doc.CreatedAt == userCreatedEvent.CreatedAt &&
                    doc.CreatedBy == userCreatedEvent.CreatedBy &&
                    doc.UpdatedAt == userCreatedEvent.UpdatedAt &&
                    doc.UpdatedBy == userCreatedEvent.UpdatedBy),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRoleCollection.Verify(
            c => c.InsertOneAsync(
                It.Is<MongoRoleDocument>(doc =>
                    doc.RoleId == userCreatedEvent.RoleId &&
                    doc.RoleName == userCreatedEvent.RoleName &&
                    doc.UserId == userCreatedEvent.UserId),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Start_WhenInvalidJsonReceived_ShouldNotInsertDocumentsAndHandleError()
    {
        // Arrange
        var invalidJsonPayload = "esto no es json valido {";

        // Act
        await _sut.Start(_mockConnection.Object);
        Assert.NotNull(_capturedConsumer);

        var deliveryArgs = new BasicDeliverEventArgs(
            "test-consumer-tag",
            2UL,
            false,
            string.Empty,
            string.Empty,
            null,
            new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(invalidJsonPayload)));

        await _capturedConsumer.HandleBasicDeliverAsync(
             deliveryArgs.ConsumerTag, deliveryArgs.DeliveryTag, deliveryArgs.Redelivered,
             deliveryArgs.Exchange, deliveryArgs.RoutingKey, deliveryArgs.BasicProperties, deliveryArgs.Body
        );

        // Assert
        _mockUserCollection.Verify(
            c => c.InsertOneAsync(It.IsAny<MongoUserDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockRoleCollection.Verify(
            c => c.InsertOneAsync(It.IsAny<MongoRoleDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Start_WhenMessageDeserializesToNullEvent_ShouldNotInsertDocuments()
    {
        // Arrange
        var jsonPayloadForNullEvent = "null";

        // Act
        await _sut.Start(_mockConnection.Object);
        Assert.NotNull(_capturedConsumer);

        var deliveryArgs = new BasicDeliverEventArgs(
            "test-consumer-tag",
            3UL,
            false,
            string.Empty,
            string.Empty,
            null,
            new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(jsonPayloadForNullEvent)));

        await _capturedConsumer.HandleBasicDeliverAsync(
            deliveryArgs.ConsumerTag, deliveryArgs.DeliveryTag, deliveryArgs.Redelivered,
            deliveryArgs.Exchange, deliveryArgs.RoutingKey, deliveryArgs.BasicProperties, deliveryArgs.Body
        );

        // Assert
        _mockUserCollection.Verify(
            c => c.InsertOneAsync(It.IsAny<MongoUserDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockRoleCollection.Verify(
            c => c.InsertOneAsync(It.IsAny<MongoRoleDocument>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}