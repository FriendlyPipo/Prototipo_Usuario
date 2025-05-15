    using MediatR;
    using MassTransit;
    using Microsoft.EntityFrameworkCore;
    using Users.Domain.Entities;
    using Users.Application.Commands;
    using Users.Infrastructure.Database;
    using Users.Core.Repositories;
    using Users.Infrastructure.Exceptions;
    using Users.Infrastructure.Interfaces;
    using Users.Application.UserValidations;
    using Users.Application.DTO.Request; 
    using Users.Infrastructure.EventBus;
    using Users.Core.Events;
    using Users.Infrastructure.EventBus.Events;
    using System.Text.Json;

namespace Users.Application.Handlers.Commands
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, string>
    {
        private readonly IUserWriteRepository _userWriteRepository;
        private readonly UserDbContext _dbContext;
        private readonly IKeycloakRepository _keycloakRepository;
        private readonly IEventBus _eventBus;

        public UpdateUserCommandHandler(IEventBus eventBus, IUserWriteRepository userWriteRepository, UserDbContext dbContext, IKeycloakRepository keycloakRepository)
        {
            _userWriteRepository = userWriteRepository;
            _dbContext = dbContext;
            _keycloakRepository = keycloakRepository;
            _eventBus = eventBus;
        } 

        public async Task<string> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var updatedUser = await _userWriteRepository.GetByIdAsync(request.Users.UserId);
            Guid roleId= Guid.Empty;
            var existingUser = updatedUser.UserEmail;
            if (updatedUser == null)
            {
                throw new UserNotFoundException($"Usuario con ID '{request.Users.UserId}' no encontrado.");
            }

            if (request.Users.UserEmail == null)
            {
                throw new NullAtributeException("El email no puede ser nulo.");
            }
            updatedUser.UpdateUserEmail(request.Users.UserEmail);

            if (request.Users.UserName != null)
            {
                updatedUser.UpdateUserName(request.Users.UserName);
            }

            if (request.Users.UserLastName != null)
            {
                updatedUser.UpdateUserLastName(request.Users.UserLastName);
            }
            
            if (request.Users.UserPhoneNumber != null)
            {
                updatedUser.UpdateUserPhoneNumber(request.Users.UserPhoneNumber);
            }

            if (request.Users.UserDirection != null)
            {
                updatedUser.UpdateUserDirection(request.Users.UserDirection);
            }

            if (request.Users.UserPassword != null)
            {
                updatedUser.UpdateUserPassword(request.Users.UserPassword);
            }

            var existingRole = await _dbContext.Role
                    .FirstOrDefaultAsync(r => r.UserId == updatedUser.UserId, cancellationToken);

                if (Enum.TryParse<UserRoleName>(request.Users.UserRole, out var newRoleName))
                {
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
                }
                else
                {
                    Console.WriteLine($"El valor del rol '{request.Users.UserRole}' no es válido.");
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
                updatedUser.UserConfirmation, 
                updatedUser.UserPassword,
                roleId, 
                request.Users.UserRole);
            _eventBus.Publish<UserUpdatedEvent>(userUpdatedEvent, "user.updated");

            // Actualizar el usuario en Keycloak
            var token = await _keycloakRepository.GetTokenAsync();
            var keycloakUserId = await _keycloakRepository.GetUserIdAsync(existingUser, token);
            Console.WriteLine($"ID de usuario de Keycloak: {keycloakUserId}");
            var KcUser = new 
            {
                username = request.Users.UserEmail,
                email = request.Users.UserEmail,
                enabled = true,
                firstName = request.Users.UserName,
                lastName = request.Users.UserLastName,  
                credentials = new [] { new { type = "password", value = request.Users.UserPassword, temporary = false } },
            };
            var updateUserResponseJson = await _keycloakRepository.UpdateUserAsync(KcUser,keycloakUserId, token);
            return "Usuario Actualizado Correctamente";
        }
    }
}
