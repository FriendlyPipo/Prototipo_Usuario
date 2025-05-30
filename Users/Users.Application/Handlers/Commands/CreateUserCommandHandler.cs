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
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, string>
    {
        private readonly IUserWriteRepository _userWriteRepository;
        private readonly UserDbContext _dbContext;
        private readonly IKeycloakRepository _keycloakRepository;
        private readonly IEventBus _eventBus;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IEventBus eventBus,
            IUserWriteRepository userWriteRepository,
            UserDbContext dbContext,
            IKeycloakRepository keycloakRepository,
            ILogger<CreateUserCommandHandler> logger)
        {
            _userWriteRepository = userWriteRepository;
            _dbContext = dbContext;
            _keycloakRepository = keycloakRepository;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {                
                var validacion = new UserValidation();
                Guid roleId = Guid.Empty;
                

                await validacion.ValidateRequest(request.Users);

                var newUser = new User(
                    request.Users.UserName,
                    request.Users.UserLastName,
                    request.Users.UserEmail,
                    request.Users.UserPhoneNumber,
                    request.Users.UserDirection
                );

                if (!string.IsNullOrEmpty(request.Users.UserRole))
                {
                    if (Enum.TryParse<UserRoleName>(request.Users.UserRole, out var roleName))
                    {
                        var newUserRole = new UserRole(roleName)
                        {
                            UserId = newUser.UserId
                        };
                        newUser.UserRoles.Add(newUserRole);
                        _dbContext.Role.Add(newUserRole);
                        roleId = newUserRole.RoleId;
                    }
                    else
                    {
                        throw new UserRoleException($"El valor del rol '{request.Users.UserRole}' no es válido.");
                    }
                }

                using (var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken))
                {
                    try
                    {
                        _logger.LogInformation("Creando usuario en base de datos");
                        await _userWriteRepository.CreateAsync(newUser);
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        // Registrar en MongoDB
                        var userCreatedEvent = new UserCreatedEvent(
                            newUser.UserId,
                            newUser.UserName,
                            newUser.UserLastName,
                            newUser.UserEmail,
                            newUser.UserPhoneNumber,
                            newUser.UserDirection,
                            newUser.CreatedAt,
                            newUser.CreatedBy,
                            newUser.UpdatedAt,
                            newUser.UpdatedBy,
                            roleId,
                            request.Users.UserRole
                        );
                        await _eventBus.Publish<UserCreatedEvent>(userCreatedEvent, "user.created");

                        // Crear usuario en Keycloak
                        var token = await _keycloakRepository.GetTokenAsync();
                        var kcUser = new
                        {
                            username = request.Users.UserEmail,
                            email = request.Users.UserEmail,
                            enabled = true,
                            firstName = request.Users.UserName,
                            lastName = request.Users.UserLastName,
                            credentials = new[] { new { type = "password", value = request.Users.UserPassword, temporary = false } },
                            requiredActions = new[] { "VERIFY_EMAIL" },
                        };

                        try
                        {
                            var createUserResponseJson = await _keycloakRepository.CreateUserAsync(kcUser, token);
                            string keycloakUserId = await _keycloakRepository.GetUserIdAsync(kcUser.username, token);

                            try
                            {
                                await _keycloakRepository.SendVerificationEmailAsync(keycloakUserId, token);
                            }
                            catch (Exception ex)
                            {   
                                await transaction.RollbackAsync(cancellationToken);
                                _logger.LogError(ex, "Error al enviar email de verificación para el usuario", keycloakUserId);
                                throw new KeycloakException("Error al enviar email de verificación", ex);
                            }

                            await transaction.CommitAsync(cancellationToken);
                            _logger.LogInformation("Usuario creado exitosamente con ID", newUser.UserId);
                            return newUser.UserId.ToString();
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            _logger.LogError(ex, "Error inesperado al crear usuario en Keycloak");
                            throw new KeycloakException("Error al crear usuario en Keycloak", ex);
                        }
                     }
                    catch (DbUpdateException ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogError(ex, "Error al guardar usuario en base de datos");
                       throw new UserException("Error al guardar usuario en base de datos", ex);
                    }   
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                throw;
            }      
        }
    }
}