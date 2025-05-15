using MediatR;
using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Application.Commands;
using Users.Core.Repositories;
using Users.Core.Events;
using Users.Infrastructure.Database;
using Users.Infrastructure.Exceptions;
using Users.Infrastructure.Interfaces;
using Users.Infrastructure.EventBus.Events;
using Users.Infrastructure.EventBus;

namespace Users.Application.Handlers.Commands
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, string>
    {
        private readonly IUserWriteRepository _userWriteRepository;
        private readonly UserDbContext _dbContext;
        private readonly IKeycloakRepository _keycloakRepository;
        private readonly IEventBus _eventBus;

        public DeleteUserCommandHandler(IEventBus eventBus,IUserWriteRepository userWriteRepository, UserDbContext dbContext, IKeycloakRepository keycloakRepository)
        {
            _userWriteRepository = userWriteRepository;
            _dbContext = dbContext;
            _keycloakRepository = keycloakRepository;
            _eventBus = eventBus;
        }

        public async Task<string> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var deletedUser = await _userWriteRepository.GetByIdAsync(request.Users.UserId);
            if (deletedUser == null)
            {
                throw new UserNotFoundException($"Usuario con ID '{request.Users.UserId}' no encontrado.");
            }
            var userDeletedEvent = new UserDeletedEvent(request.Users.UserId);
            await _userWriteRepository.DeleteAsync(deletedUser.UserId);
            var deletedFromDb = await _dbContext.SaveChangesAsync() > 0;  
            _eventBus.Publish<UserDeletedEvent>(userDeletedEvent, "user.deleted");            

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
                    _eventBus.Publish<UserDeletedEvent>(userDeletedEvent, "user.deleted"); 
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