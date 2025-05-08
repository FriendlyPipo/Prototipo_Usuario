using MediatR;
using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Application.Commands;
using Users.Core.Repositories;
using Users.Infrastructure.Database;
using Users.Infrastructure.Exceptions;
using Users.Infrastructure.Interfaces;

namespace Users.Application.Handlers.Commands
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly UserDbContext _dbContext;
        private readonly IKeycloakRepository _keycloakRepository;

        public DeleteUserCommandHandler(IUserRepository userRepository, UserDbContext dbContext, IKeycloakRepository keycloakRepository)
        {
            _userRepository = userRepository;
            _dbContext = dbContext;
            _keycloakRepository = keycloakRepository;
        }

        public async Task<string> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var deletedUser = await _userRepository.GetByIdAsync(request.Users.UserId);
            if (deletedUser == null)
            {
                throw new UserNotFoundException($"Usuario con ID '{request.Users.UserId}' no encontrado.");
            }
            await _userRepository.DeleteAsync(deletedUser.UserId);
            var deletedFromDb = await _dbContext.SaveChangesAsync() > 0;
            if (deletedFromDb)
            {
                var token = await _keycloakRepository.GetTokenAsync();
                string? keycloakUserId = null; 
                if (!string.IsNullOrEmpty(deletedUser.UserEmail))
                {
                    keycloakUserId = await _keycloakRepository.GetUserIdAsync(deletedUser.UserEmail, token);
                }
                if (keycloakUserId != null)
                {
                    var keycloakDisableResult = await _keycloakRepository.DisableUserAsync(keycloakUserId, token);

                    if (!keycloakDisableResult)
                    {
                        Console.WriteLine($"Error al deshabilitar usuario con ID '{keycloakUserId}' de Keycloak.");
                    }
                }
                else
                {
                    Console.WriteLine($"No se pudo encontrar el usuario en Keycloak por username '{deletedUser.UserName}' o email '{deletedUser.UserEmail}'.");
                }

                return "Usuario Borrado Correctamente";
            }
            else
            {
                throw new DbUpdateException($"Error al eliminar el usuario con ID '{request.Users.UserId}' de la base de datos.");
            }
        }
    }
}