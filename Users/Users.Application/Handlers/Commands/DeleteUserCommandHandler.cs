using MediatR;
using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Application.Commands;
using Users.Core.Repositories;
using Users.Core.Events;
using Users.Infrastructure.Database;
using Users.Infrastructure.Exceptions;
using Users.Infrastructure.EventBus.Events;
using Users.Infrastructure.EventBus;
using Microsoft.Extensions.Logging;

namespace Users.Application.Handlers.Commands
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, string>
    {
        private readonly IUserWriteRepository _userWriteRepository;
        private readonly UserDbContext _dbContext;
        private readonly IKeycloakRepository _keycloakRepository;
        private readonly IEventBus _eventBus;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(
            IEventBus eventBus,
            IUserWriteRepository userWriteRepository,
            UserDbContext dbContext,
            IKeycloakRepository keycloakRepository,
            ILogger<DeleteUserCommandHandler> logger)
        {
            _userWriteRepository = userWriteRepository;
            _dbContext = dbContext;
            _keycloakRepository = keycloakRepository;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task<string> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Intentando eliminar usuario con ID: {UserId}", request.Users.UserId);

            var userToDelete = await _userWriteRepository.GetByIdAsync(request.Users.UserId);
            if (userToDelete == null)
            {
                throw new UserNotFoundException($"Usuario con ID '{request.Users.UserId}' no encontrado.");
            }
        
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _userWriteRepository.DeleteAsync(userToDelete.UserId);
                int changes = await _dbContext.SaveChangesAsync(cancellationToken);
                
                if (changes == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("No se pudo confirmar la eliminación del usuario. UserId: {UserId}, Filas afectadas: 0", request.Users.UserId);
                    throw new UserException("No se pudo confirmar la eliminación del usuario de la base de datos (0 filas afectadas).");
                }

                var userDeletedEvent = new UserDeletedEvent(userToDelete.UserId);
                await _eventBus.Publish<UserDeletedEvent>(userDeletedEvent, "user.deleted");

                var token = await _keycloakRepository.GetTokenAsync();
                string? keycloakUserId = await _keycloakRepository.GetUserIdAsync(userToDelete.UserEmail, token);

                if (keycloakUserId == null)
                {
                    throw new KeycloakException($"Usuario '{userToDelete.UserEmail}' no encontrado en Keycloak.");
                }

                var keycloakDisableResult = await _keycloakRepository.DisableUserAsync(keycloakUserId, token);
                if (!keycloakDisableResult)
                {
                    throw new KeycloakException($"Fallo al deshabilitar usuario en Keycloak (ID: {keycloakUserId}).");
                }

                _logger.LogInformation("Usuario de Keycloak deshabilitado exitosamente. KeycloakId: {KeycloakId}, UserId: {UserId}", 
                    keycloakUserId, userToDelete.UserId);

                await transaction.CommitAsync(cancellationToken);
                return "Usuario eliminado correctamente.";
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error al intentar eliminar el usuario de la base de datos. UserId: {UserId}", request.Users.UserId);
                throw new UserException("Error al eliminar el usuario de la base de datos.", ex);
            }
            catch (KeycloakException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error en la operación de Keycloak para el usuario. UserId: {UserId}", request.Users.UserId);
                throw;
            }
            catch (Exception ex) when (ex is not UserException) // Excluimos UserException para no capturar el caso de changes == 0
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error inesperado al eliminar el usuario. UserId: {UserId}", request.Users.UserId);
                throw new UserException("Error inesperado al procesar la eliminación del usuario.", ex);
            }
        }
    }
}
