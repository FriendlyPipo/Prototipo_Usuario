using MediatR;
using Users.Domain.Entities;
using Users.Application.Commands;
using Users.Core.Repositories;
using Users.Infrastructure.Exceptions;

namespace Users.Application.Handlers.Commands
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, string>
    {
        private readonly IKeycloakRepository _keycloakRepository; 

        public ForgotPasswordCommandHandler(IKeycloakRepository keycloakRepository)
        {
            _keycloakRepository = keycloakRepository;
        }

        public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var userEmail = request.Users.UserEmail;

            string? keycloakId = await _keycloakRepository.GetUserIdAsync(userEmail, await _keycloakRepository.GetTokenAsync());
            try
            {
                var token = await _keycloakRepository.GetTokenAsync();
                await _keycloakRepository.SendPasswordResetEmailAsync(keycloakId, token);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al solicitar envío de email de restablecimiento de contraseña a Keycloak para usuario {keycloakId}: {ex.Message}");
            }

            return "Recibirás un enlace para restablecer tu contraseña.";
        }
    }
}