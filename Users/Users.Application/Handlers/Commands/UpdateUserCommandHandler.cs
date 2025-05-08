using MediatR;
using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Application.Commands;
using Users.Core.Repositories;
using Users.Infrastructure.Database;
using Users.Infrastructure.Exceptions;
using Users.Infrastructure.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Users.Application.Handlers.Commands
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly UserDbContext _dbContext;
        private readonly IKeycloakRepository _keycloakRepository;

        public UpdateUserCommandHandler(IUserRepository userRepository, UserDbContext dbContext, IKeycloakRepository keycloakRepository)
        {
            _userRepository = userRepository;
            _dbContext = dbContext;
            _keycloakRepository = keycloakRepository;
        } 

        public async Task<string> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var updatedUser = await _userRepository.GetByIdAsync(request.Users.UserId);
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
                    }
                }
                else
                {
                    Console.WriteLine($"El valor del rol '{request.Users.UserRole}' no es válido.");
                }


            await _userRepository.UpdateAsync(updatedUser);
            await _dbContext.SaveChangesAsync(cancellationToken);

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
