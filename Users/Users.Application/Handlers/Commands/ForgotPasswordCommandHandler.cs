using MediatR;
using Users.Application.Commands;
using Users.Core.Repositories;
using Users.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace Users.Application.Handlers.Commands
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, string>
    {
        private readonly IKeycloakRepository _keycloakRepository; 
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        public ForgotPasswordCommandHandler(IKeycloakRepository keycloakRepository, ILogger<ForgotPasswordCommandHandler> logger)
        {
            _keycloakRepository = keycloakRepository;
            _logger = logger;
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
                _logger.LogError(ex, "Error al solicitar envío de email de de contraseña");
                throw new KeycloakException("Error al envíar email de restablecimiento de contraseña", ex);
            }

            return "Recibirás un enlace para restablecer tu contraseña.";
        }
    }
}