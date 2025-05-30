using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Users.Infrastructure.EventBus;
using Xunit;

namespace Users.Test.Users.Infrastructure.Test.EventBus
{
    public class RabbitMQChannelFactoryTest
    {
        private readonly Mock<IOptions<RabbitMQSetting>> _mockOptions;
        private readonly RabbitMQSetting _settings;

        public RabbitMQChannelFactoryTest()
        {
            _settings = new RabbitMQSetting
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _mockOptions = new Mock<IOptions<RabbitMQSetting>>();
            _mockOptions.Setup(x => x.Value).Returns(_settings);
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            IOptions<RabbitMQSetting> nullOptions = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RabbitMQChannelFactory(nullOptions));
        }

        [Fact]
        public void Constructor_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var mockOptionsWithNullValue = new Mock<IOptions<RabbitMQSetting>>();
            mockOptionsWithNullValue.Setup(x => x.Value).Returns((RabbitMQSetting)null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RabbitMQChannelFactory(mockOptionsWithNullValue.Object));
        }

        [Fact]
        public async Task CreateChannelAsync_WhenConnectionExists_ReusesConnection()
        {
            // Arrange
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IChannel>();
            
            mockConnection.Setup(x => x.IsOpen).Returns(true);
            mockConnection.Setup(x => x.CreateChannelAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockChannel.Object);

            var factory = new RabbitMQChannelFactory(_mockOptions.Object);
            var connectionField = typeof(RabbitMQChannelFactory).GetField("_connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            connectionField.SetValue(factory, mockConnection.Object);

            // Act
            var channel1 = await factory.CreateChannelAsync();
            var channel2 = await factory.CreateChannelAsync();

            // Assert
            Assert.NotNull(channel1);
            Assert.NotNull(channel2);
            mockConnection.Verify(x => x.CreateChannelAsync(null, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task DisposeAsync_ClosesAndDisposesConnection()
        {
            // Arrange
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(x => x.IsOpen).Returns(true);

            var factory = new RabbitMQChannelFactory(_mockOptions.Object);
            var connectionField = typeof(RabbitMQChannelFactory).GetField("_connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            connectionField.SetValue(factory, mockConnection.Object);

            // Act
            await factory.DisposeAsync();

            // Assert
            mockConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task CreateChannelAsync_WhenConnectionClosed_DisposesOldConnection()
        {
            // Arrange
            var mockOldConnection = new Mock<IConnection>();
            mockOldConnection.Setup(x => x.IsOpen).Returns(false);

            var factory = new RabbitMQChannelFactory(_mockOptions.Object);
            var connectionField = typeof(RabbitMQChannelFactory).GetField("_connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            connectionField.SetValue(factory, mockOldConnection.Object);

            // Act & Assert
            await Assert.ThrowsAsync<BrokerUnreachableException>(() => factory.CreateChannelAsync());
            mockOldConnection.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenConnectionNull_DoesNotThrow()
        {
            // Arrange
            var factory = new RabbitMQChannelFactory(_mockOptions.Object);

            // Act & Assert
            var exception = await Record.ExceptionAsync(() => factory.DisposeAsync().AsTask());
            Assert.Null(exception);
        }
    }
}
