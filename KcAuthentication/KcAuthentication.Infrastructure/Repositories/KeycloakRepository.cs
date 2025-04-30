
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using KcAuthentication.Common.Dtos;
    using KcAuthentication.Core.Interfaces;
    using KcAuthentication.Common.Exceptions;
    using Microsoft.Extensions.Configuration;

    namespace KcAuthentication.Infrastructure.Repositories
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
                var tokenUrl = $"{_configuration["Keycloak:BaseUrl"]}/protocol/openid-connect/token";
                var clientId = _configuration.GetValue<string>("Keycloak:ClientId") ?? throw new KeyNotFoundException("Configuration 'Keycloak:ClientId' is missing.");
                var clientSecret = _configuration.GetValue<string>("Keycloak:ClientSecret") ?? throw new KeyNotFoundException("Configuration 'Keycloak:ClientSecret' is missing.");

                var requestBody = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "grant_type", "client_credentials" }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(requestBody)
                };

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<TokenDto>(content);
                    return tokenResponse?.AuthenticationToken ?? string.Empty;
                }

                throw new KeycloakException("Failed to retrieve token from Keycloak.");
            } 

            public async Task<string> CreateUserAsync(CreateUserDto user, string token)
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

            public async Task AssignRoleToUserAsync(string email, string token)
            {
                var url = $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{email}/role-mappings/realm";
                var requestBody = new StringContent(JsonSerializer.Serialize(new { name = "user" }), Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = requestBody
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new KeycloakException("Failed to assign role to user in Keycloak.");
                }  
            }
        }
    }