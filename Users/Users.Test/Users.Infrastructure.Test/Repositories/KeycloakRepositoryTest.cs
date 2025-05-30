using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Users.Infrastructure.Exceptions;
using Users.Infrastructure.Repositories;
using Xunit;

namespace Users.Test.Users.Infrastructure.Test
{
    public class KeycloakRepositoryTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly KeycloakRepository _keycloakRepository;
        private readonly Mock<IConfigurationSection> _clientIdSection;
        private readonly Mock<IConfigurationSection> _clientSecretSection;

        public KeycloakRepositoryTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _clientIdSection = new Mock<IConfigurationSection>();
            _clientSecretSection = new Mock<IConfigurationSection>();

            // Setup configuration sections
            _clientIdSection.Setup(s => s.Value).Returns("test-client");
            _clientSecretSection.Setup(s => s.Value).Returns("test-secret");

            // Setup configuration
            _configurationMock.Setup(x => x["Keycloak:BaseUrl"]).Returns("http://keycloak-test");
            _configurationMock.Setup(x => x["Keycloak:Realm"]).Returns("test-realm");
            _configurationMock.Setup(x => x.GetSection("Keycloak:ClientId")).Returns(_clientIdSection.Object);
            _configurationMock.Setup(x => x.GetSection("Keycloak:ClientSecret")).Returns(_clientSecretSection.Object);

            _keycloakRepository = new KeycloakRepository(_httpClient, _configurationMock.Object);
        }

        [Fact]
        public async Task GetTokenAsync_Success_ReturnsToken()
        {
            // Arrange
            var expectedToken = "test-token";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"access_token\": \"{expectedToken}\"}}")
            };

            SetupHttpMessageHandler(response);

            // Act
            var result = await _keycloakRepository.GetTokenAsync();

            // Assert
            Assert.Equal(expectedToken, result);
        }

        [Fact]
        public async Task GetTokenAsync_Failure_ThrowsKeycloakException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Invalid client credentials")
            };

            SetupHttpMessageHandler(response);

            // Act & Assert
            await Assert.ThrowsAsync<KeycloakException>(() => _keycloakRepository.GetTokenAsync());
        }

        [Fact]
        public async Task GetTokenAsync_InvalidJson_ThrowsKeycloakException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Invalid JSON")
            };

            SetupHttpMessageHandler(response);

            // Act & Assert
            await Assert.ThrowsAsync<KeycloakException>(() => _keycloakRepository.GetTokenAsync());
        }

        [Fact]
        public async Task CreateUserAsync_Success_ReturnsResponse()
        {
            // Arrange
            var user = new { username = "testuser", email = "test@test.com" };
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"id\": \"user-id\"}")
            };

            SetupHttpMessageHandler(response);

            // Act
            var result = await _keycloakRepository.CreateUserAsync(user, "test-token");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CreateUserAsync_Failure_ThrowsKeycloakException()
        {
            // Arrange
            var user = new { username = "testuser", email = "test@test.com" };
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("User already exists")
            };

            SetupHttpMessageHandler(response);

            // Act & Assert
            await Assert.ThrowsAsync<KeycloakException>(() => 
                _keycloakRepository.CreateUserAsync(user, "test-token"));
        }

        [Fact]
        public async Task AssignRoleToUserAsync_Success()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            SetupHttpMessageHandler(response);

            // Act & Assert
            await _keycloakRepository.AssignRoleToUserAsync("user-id", "test-role", "test-token");
        }

        [Fact]
        public async Task AssignRoleToUserAsync_Failure_ThrowsKeycloakException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Role assignment failed")
            };

            SetupHttpMessageHandler(response);

            // Act & Assert
            await Assert.ThrowsAsync<KeycloakException>(() => 
                _keycloakRepository.AssignRoleToUserAsync("user-id", "test-role", "test-token"));
        }

        [Fact]
        public async Task DisableUserAsync_Success_ReturnsTrue()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            SetupHttpMessageHandler(response);

            // Act
            var result = await _keycloakRepository.DisableUserAsync("user-id", "test-token");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DisableUserAsync_Failure_ThrowsKeycloakException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("User disable failed")
            };

            SetupHttpMessageHandler(response);

            // Act & Assert
            await Assert.ThrowsAsync<KeycloakException>(() => 
                _keycloakRepository.DisableUserAsync("user-id", "test-token"));
        }

        [Fact]
        public async Task GetUserIdAsync_Success_ReturnsUserId()
        {
            // Arrange
            var expectedUserId = "test-user-id";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"[{{\"id\": \"{expectedUserId}\"}}]")
            };

            SetupHttpMessageHandler(response);

            // Act
            var result = await _keycloakRepository.GetUserIdAsync("username", "test-token");

            // Assert
            Assert.Equal(expectedUserId, result);
        }

        [Fact]
        public async Task GetUserIdAsync_UserNotFound_ReturnsNull()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            };

            SetupHttpMessageHandler(response);

            // Act
            var result = await _keycloakRepository.GetUserIdAsync("username", "test-token");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserIdAsync_InvalidJson_ThrowsKeycloakException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Invalid JSON")
            };

            SetupHttpMessageHandler(response);

            // Act & Assert
            await Assert.ThrowsAsync<KeycloakException>(() => 
                _keycloakRepository.GetUserIdAsync("username", "test-token"));
        }

        [Fact]
        public async Task SendVerificationEmailAsync_Success()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            SetupHttpMessageHandler(response);

            // Act & Assert
            await _keycloakRepository.SendVerificationEmailAsync("user-id", "test-token");
        }

        [Fact]
        public async Task SendVerificationEmailAsync_Failure_ThrowsException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            SetupHttpMessageHandler(response);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => 
                _keycloakRepository.SendVerificationEmailAsync("user-id", "test-token"));
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_Success()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            SetupHttpMessageHandler(response);

            // Act & Assert
            await _keycloakRepository.SendPasswordResetEmailAsync("user-id", "test-token");
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_Failure_ThrowsKeycloakException()
        {
            // Arrange
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            SetupHttpMessageHandler(response);

            // Act & Assert
            await Assert.ThrowsAsync<KeycloakException>(() => 
                _keycloakRepository.SendPasswordResetEmailAsync("user-id", "test-token"));
        }

        [Fact]
        public async Task UpdateUserAsync_Success_ReturnsUpdatedUser()
        {
            // Arrange
            var user = new { username = "testuser", email = "updated@test.com" };
            var expectedResponse = "{\"id\": \"user-id\", \"email\": \"updated@test.com\"}";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expectedResponse)
            };

            SetupHttpMessageHandler(response);

            // Act
            var result = await _keycloakRepository.UpdateUserAsync(user, "user-id", "test-token");

            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task UpdateUserAsync_Failure_ThrowsKeycloakException()
        {
            // Arrange
            var user = new { username = "testuser", email = "updated@test.com" };
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("User update failed")
            };

            SetupHttpMessageHandler(response);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeycloakException>(() => 
                _keycloakRepository.UpdateUserAsync(user, "user-id", "test-token"));
            
            Assert.Contains("Failed to update user in Keycloak", exception.Message);
            Assert.Contains(HttpStatusCode.BadRequest.ToString(), exception.Message);
        }

        private void SetupHttpMessageHandler(HttpResponseMessage response)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);
        }
    }
} 