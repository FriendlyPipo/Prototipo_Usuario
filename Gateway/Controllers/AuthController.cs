using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Gateway.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var client = _httpClientFactory.CreateClient();

            var keycloakUrl = _configuration["Keycloak:TokenUrl"];
            var clientId = _configuration["Keycloak:ClientId"];
            var clientSecret = _configuration["Keycloak:ClientSecret"];

            var form = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "password" },
                { "username", model.Username },
                { "password", model.Password }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, keycloakUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return Unauthorized("Credenciales inválidas");
            }

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            var client = _httpClientFactory.CreateClient();

            var adminToken = await GetAdminAccessTokenAsync(); // Necesitas esto con client credentials grant

            var searchRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users?email={model.Email}");
            searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var userResponse = await client.SendAsync(searchRequest);

            var userListContent = await userResponse.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<dynamic>>(userListContent);

            if (users == null || users.Count == 0 || users[0].id == null)
                return NotFound("No se encontró un usuario con ese correo.");

            string userId = users[0].id;

            // Trigger el correo de recuperación
            var redirectWithUserId = $"{_configuration["FrontendRedirectUri"]}?user={userId}";

            var resetRequest = new HttpRequestMessage(HttpMethod.Put,
                $"{_configuration["Keycloak:BaseUrl"]}/admin/realms/{_configuration["Keycloak:Realm"]}/users/{userId}/execute-actions-email?client_id={_configuration["Keycloak:ClientId"]}&redirect_uri={Uri.EscapeDataString(redirectWithUserId)}");

            resetRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            resetRequest.Content = new StringContent(JsonConvert.SerializeObject(new[] { "UPDATE_PASSWORD" }), Encoding.UTF8, "application/json");

            var resetResponse = await client.SendAsync(resetRequest);

            if (!resetResponse.IsSuccessStatusCode)
                return StatusCode((int)resetResponse.StatusCode, "Error al enviar correo.");

            return Ok("Correo de recuperación enviado.");
        }

        private async Task<string> GetAdminAccessTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();

            var form = new Dictionary<string, string>
            {
                { "client_id", _configuration["Keycloak:ClientId"] },
                { "client_secret", _configuration["Keycloak:ClientSecret"] },
                { "grant_type", "password" },
                { "username", _configuration["Keycloak:AdminUsername"] },
                { "password", _configuration["Keycloak:AdminPassword"] }
            };

            var response = await client.PostAsync(_configuration["Keycloak:TokenUrl"], new FormUrlEncodedContent(form));
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error al obtener el token de administrador");
            }

            var content = await response.Content.ReadAsStringAsync();
            dynamic tokenData = JsonConvert.DeserializeObject(content);
            return tokenData.access_token;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
        {
            var client = _httpClientFactory.CreateClient();

            var keycloakUrl = _configuration["Keycloak:TokenUrl"];
            var clientId = _configuration["Keycloak:ClientId"];
            var clientSecret = _configuration["Keycloak:ClientSecret"];

            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("refresh_token", request.RefreshToken)
            });

            var response = await client.PostAsync(keycloakUrl, body);
            if (!response.IsSuccessStatusCode)
            {
                return Unauthorized("Invalid refresh token.");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return Content(responseContent, "application/json");
        }

    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; }
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

}
