using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using Users.Domain.Entities;
using Users.Infrastructure.Database;
using Users.Infrastructure.Settings;
using Xunit;

namespace Users.Test.Users.Infrastructure.Test.DataBase
{
    public class MongoDbContextTest
    {
        private readonly Mock<IOptions<MongoDBSettings>> _mockOptions;
        private readonly MongoDBSettings _settings;
        private readonly Mock<IMongoClient> _mockMongoClient;
        private readonly Mock<IMongoDatabase> _mockDatabase;

        public MongoDbContextTest()
        {
            _settings = new MongoDBSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "TestDb"
            };

            _mockOptions = new Mock<IOptions<MongoDBSettings>>();
            _mockOptions.Setup(x => x.Value).Returns(_settings);
        }

        [Fact]
        public void Constructor_WithValidSettings_CreatesContext()
        {
            // Act
            var context = new MongoDbContext(_mockOptions.Object);

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.User);
            Assert.NotNull(context.Role);
        }

        [Fact]
        public void Constructor_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            IOptions<MongoDBSettings> nullSettings = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new MongoDbContext(nullSettings));
            Assert.Equal("mongoDbSettings", exception.ParamName);
            Assert.Contains("Configuración de MongoDB no proporcionada", exception.Message);
        }

        [Fact]
        public void Constructor_WithNullSettingsValue_ThrowsArgumentNullException()
        {
            // Arrange
            var mockOptionsWithNullValue = new Mock<IOptions<MongoDBSettings>>();
            mockOptionsWithNullValue.Setup(x => x.Value).Returns((MongoDBSettings)null);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new MongoDbContext(mockOptionsWithNullValue.Object));
            Assert.Equal("mongoDbSettings", exception.ParamName);
            Assert.Contains("Configuración de MongoDB no proporcionada", exception.Message);
        }

        [Fact]
        public void User_ReturnsMongoCollection()
        {
            // Arrange
            var context = new MongoDbContext(_mockOptions.Object);

            // Act
            var userCollection = context.User;

            // Assert
            Assert.NotNull(userCollection);
            Assert.IsAssignableFrom<IMongoCollection<MongoUserDocument>>(userCollection);
            Assert.Equal("User", userCollection.CollectionNamespace.CollectionName);
        }

        [Fact]
        public void Role_ReturnsMongoCollection()
        {
            // Arrange
            var context = new MongoDbContext(_mockOptions.Object);

            // Act
            var roleCollection = context.Role;

            // Assert
            Assert.NotNull(roleCollection);
            Assert.IsAssignableFrom<IMongoCollection<MongoRoleDocument>>(roleCollection);
            Assert.Equal("Role", roleCollection.CollectionNamespace.CollectionName);
        }

        [Theory]
        [InlineData("")]
        public void Constructor_WithEmptyConnectionString_ThrowsMongoConfigurationException(string connectionString)
        {
            // Arrange
            var invalidSettings = new MongoDBSettings
            {
                ConnectionString = connectionString,
                DatabaseName = "TestDb"
            };
            var mockOptionsWithInvalidSettings = new Mock<IOptions<MongoDBSettings>>();
            mockOptionsWithInvalidSettings.Setup(x => x.Value).Returns(invalidSettings);

            // Act & Assert
            Assert.Throws<MongoConfigurationException>(() => new MongoDbContext(mockOptionsWithInvalidSettings.Object));
        }

        [Fact]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange
            var invalidSettings = new MongoDBSettings
            {
                ConnectionString = null,
                DatabaseName = "TestDb"
            };
            var mockOptionsWithInvalidSettings = new Mock<IOptions<MongoDBSettings>>();
            mockOptionsWithInvalidSettings.Setup(x => x.Value).Returns(invalidSettings);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new MongoDbContext(mockOptionsWithInvalidSettings.Object));
            Assert.Equal("connectionString", exception.ParamName);
        }

        [Theory]
        [InlineData("")]
        public void Constructor_WithEmptyDatabaseName_ThrowsArgumentException(string databaseName)
        {
            // Arrange
            var invalidSettings = new MongoDBSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = databaseName
            };
            var mockOptionsWithInvalidSettings = new Mock<IOptions<MongoDBSettings>>();
            mockOptionsWithInvalidSettings.Setup(x => x.Value).Returns(invalidSettings);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MongoDbContext(mockOptionsWithInvalidSettings.Object));
        }

        [Fact]
        public void Constructor_WithNullDatabaseName_ThrowsArgumentNullException()
        {
            // Arrange
            var invalidSettings = new MongoDBSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = null
            };
            var mockOptionsWithInvalidSettings = new Mock<IOptions<MongoDBSettings>>();
            mockOptionsWithInvalidSettings.Setup(x => x.Value).Returns(invalidSettings);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new MongoDbContext(mockOptionsWithInvalidSettings.Object));
            Assert.Equal("databaseName", exception.ParamName);
        }
    }
}
