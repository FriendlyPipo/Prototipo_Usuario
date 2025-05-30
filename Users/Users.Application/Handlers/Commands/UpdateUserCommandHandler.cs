    using MediatR;
    using MassTransit;
    using Microsoft.EntityFrameworkCore;
    using Users.Domain.Entities;
    using Users.Application.Commands;
    using Users.Infrastructure.Database;
    using Users.Core.Repositories;
    using Users.Infrastructure.Exceptions;
    using Users.Application.UserValidations;
    using Users.Application.DTO.Request; 
    using Users.Infrastructure.EventBus;
    using Users.Core.Events;
    using Users.Infrastructure.EventBus.Events;
    using System.Text.Json;
    using Microsoft.Extensions.Logging;

namespace Users.Application.Handlers.Commands
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, string>
    {
        private readonly IUserWriteRepository _userWriteRepository;
        private readonly UserDbContext _dbContext;
        private readonly IKeycloakRepository _keycloakRepository;
        private readonly IEventBus _eventBus;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(IEventBus eventBus, IUserWriteRepository userWriteRepository, UserDbContext dbContext, IKeycloakRepository keycloakRepository, ILogger<UpdateUserCommandHandler> logger)
        {
            _userWriteRepository = userWriteRepository;
            _dbContext = dbContext;
            _keycloakRepository = keycloakRepository;
            _eventBus = eventBus;
            _logger = logger;
        } 

        public async Task<string> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {

            if (!Enum.TryParse<UserRoleName>(request.Users.UserRole, out var newRoleName))
            {
                throw new UserRoleException($"El valor del rol '{request.Users.UserRole}' no es válido.");
            }

            var updatedUser = await _userWriteRepository.GetByIdAsync(request.Users.UserId);
            var existingUser = updatedUser.UserEmail;
            Guid roleId = Guid.Empty;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                updatedUser.UpdateUserEmail(request.Users.UserEmail);
                if (request.Users.UserName != null) updatedUser.UpdateUserName(request.Users.UserName);
                if (request.Users.UserLastName != null) updatedUser.UpdateUserLastName(request.Users.UserLastName);
                if (request.Users.UserPhoneNumber != null) updatedUser.UpdateUserPhoneNumber(request.Users.UserPhoneNumber);
                if (request.Users.UserDirection != null) updatedUser.UpdateUserDirection(request.Users.UserDirection);

                var existingRole = await _dbContext.Role
                    .FirstOrDefaultAsync(r => r.UserId == updatedUser.UserId, cancellationToken);

                if (existingRole.RoleName != newRoleName)
                {
                    _dbContext.Role.Remove(existingRole);
                    var newUserRole = new UserRole(newRoleName)
                    {
                        UserId = updatedUser.UserId,
                    };
                    updatedUser.UserRoles.Add(newUserRole);
                    _dbContext.Role.Add(newUserRole);
                    roleId = newUserRole.RoleId;
                }
                else
                {
                    roleId = existingRole.RoleId;
                }

                await _userWriteRepository.UpdateAsync(updatedUser);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var userUpdatedEvent = new UserUpdatedEvent(
                    updatedUser.UserId,
                    updatedUser.UserName, 
                    updatedUser.UserLastName, 
                    updatedUser.UserEmail, 
                    updatedUser.UserPhoneNumber,
                    updatedUser.UserDirection, 
                    updatedUser.CreatedAt,
                    updatedUser.CreatedBy, 
                    updatedUser.UpdatedAt, 
                    updatedUser.UpdatedBy,
                    roleId, 
                    request.Users.UserRole);

                _eventBus.Publish<UserUpdatedEvent>(userUpdatedEvent, "user.updated");

                var token = await _keycloakRepository.GetTokenAsync();
                var keycloakUserId = await _keycloakRepository.GetUserIdAsync(existingUser, token);

                if (string.IsNullOrEmpty(keycloakUserId))
                {
                    throw new KeycloakException($"No se encontró el usuario en Keycloak con el email: {existingUser}");
                }
                
                object kcUser;
                if (request.Users.UserPassword != null)
                {
                    kcUser = new
                    {
                        username = request.Users.UserEmail,
                        email = request.Users.UserEmail,
                        enabled = true,
                        firstName = request.Users.UserName ?? updatedUser.UserName,
                        lastName = request.Users.UserLastName ?? updatedUser.UserLastName,
                        credentials = new[] { new { type = "password", value = request.Users.UserPassword, temporary = false } }
                    };
                }
                else
                {
                    kcUser = new
                    {
                        username = request.Users.UserEmail,
                        email = request.Users.UserEmail,
                        enabled = true,
                        firstName = request.Users.UserName ?? updatedUser.UserName,
                        lastName = request.Users.UserLastName ?? updatedUser.UserLastName
                    };
                }

                try 
                {
                    await _keycloakRepository.UpdateUserAsync(kcUser, keycloakUserId, token);
                }
                catch (KeycloakException kex)
                {
                    _logger.LogError(kex, "Error al actualizar usuario en Keycloak. Usuario: {@User}", new { 
                        Email = request.Users.UserEmail, 
                        KeycloakId = keycloakUserId 
                    });
                    throw;
                }

                await transaction.CommitAsync(cancellationToken);
                
                return "Usuario Actualizado Correctamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la actualización del usuario");
                await transaction.RollbackAsync(cancellationToken);

                if (ex is KeycloakException || 
                    ex is UserNotFoundException || 
                    ex is UserRoleException)
                {
                    throw;
                }

                throw new UserException("Error al actualizar usuario", ex);
            }
        }
    }
}

