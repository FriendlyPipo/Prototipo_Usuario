
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Users.Core.Repositories;
    using Users.Infrastructure.Exceptions;
    using Microsoft.Extensions.Configuration;
    using System.Net;

namespace Users.Infrastructure.Repositories
{
    public class KeycloakRepository : IKeycloakRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public KeycloakRepository(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetTokenAsync()
        {
            var url = $"{_configuration["Keycloak:BaseUrl"]}/realms/Artened/protocol/openid-connect/token";
            var clientId = _configuration.GetValue<string>("Keycloak:ClientId") ?? throw new KeyNotFoundException("Configuration 'Keycloak:ClientId' is missing.");
            var clientSecret = _configuration.GetValue<string>("Keycloak:ClientSecret") ?? throw new KeyNotFoundException("Configuration 'Keycloak:ClientSecret' is missing.");

            var requestBody = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "grant_type", "client_credentials" }
                };

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    using var document = JsonDocument.Parse(content);
                    if (document.RootElement.TryGetProperty("access_token", out var accessTokenElement))
                    {
                        return accessTokenElement.GetString() ?? string.Empty;
                    }
                    else
                    {
                        throw new KeycloakException("Response from Keycloak does not contain 'access_token'.");
                    }
                }
                catch (JsonException ex)
                {
                    throw new KeycloakException($"Error deserializing JSON: {ex.Message}.  Response content: {content}");
                }

            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new KeycloakException($"Failed to retrieve token from Keycloak. Status Code: {response.StatusCode}, Response: {errorContent}");
        }

        public async Task<string> CreateUserAsync(object user, string token)
        {
            var url = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users";
            var requestBody = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = requestBody
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new KeycloakException("Failed to create user in Keycloak.");
        }

        public async Task AssignRoleToUserAsync(string keycloakUserId, string role, string token)
        {
            var url = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{keycloakUserId}/role-mappings/realm";
            var requestBody = new StringContent(JsonSerializer.Serialize(new[] { new { name = role } }), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = requestBody
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new KeycloakException($"Failed to assign role '{role}' to user with ID '{keycloakUserId}' in Keycloak. Status code: {response.StatusCode}");
            }
        }


        public async Task<string> UpdateUserAsync(object user, string keycloakUserId, string token)
        {
            var url = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{keycloakUserId}";
            var requestBody = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = requestBody
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new KeycloakException($"Failed to update user in Keycloak. Status code: {response.StatusCode}");
        }

        public async Task<bool> DisableUserAsync(string keycloakUserId, string token)
        {
            var url = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{keycloakUserId}";
            var requestBody = new StringContent(JsonSerializer.Serialize(new { enabled = false }), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = requestBody
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new KeycloakException($"Failed to disable user with ID '{keycloakUserId}' in Keycloak. Status code: {response.StatusCode}, Error: {errorContent}");
        }

        public async Task<string?> GetUserIdAsync(string keycloakUserId, string token)
        {
            var url = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users?username={keycloakUserId}&exact=true";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    var users = JsonDocument.Parse(content).RootElement.EnumerateArray();
                    if (users.MoveNext())
                    {
                        var user = users.Current;
                        if (user.TryGetProperty("id", out var idProperty))
                        {
                            return idProperty.GetString();
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (JsonException ex)
                {
                    throw new KeycloakException($"Error deserializing Keycloak response: {ex.Message}. Response content: {content}");
                }
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new KeycloakException($"Error al buscar usuario por username '{keycloakUserId}' en Keycloak. Status code: {response.StatusCode}, Error: {errorContent}");
            }
        }

        public async Task SendVerificationEmailAsync(string keycloakUserId, string token)
        {
            var url = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{keycloakUserId}/send-verify-email";
            var content = new StringContent("{}", Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task SendPasswordResetEmailAsync(string keycloakUserId, string token)
        {
            var url = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{keycloakUserId}/execute-actions-email";
            var actions = new[] { "UPDATE_PASSWORD" };
            var requestBody = new StringContent(JsonSerializer.Serialize(actions), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = requestBody
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new KeycloakException($"Failed to send password reset email to user with ID '{keycloakUserId}' in Keycloak. Status code: {response.StatusCode}");
            }

        }   
    }
}