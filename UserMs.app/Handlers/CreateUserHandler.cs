// Por probar 
using MediatR;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserMs.Domain.Entities;
using UserMs.app.Commands;
using UserMs.Infra.Data;
using UserMs.Domain.Interfaces;
using UserMs.Infra.Exceptions;
using UserMs.Infra.Interfaces;
using UserMs.app.UserValidations;

namespace UserMs.app.Handlers
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand,Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;

        public CreateUserHandler(IUserRepository userRepository, IAuthService authService)
        {
            _authService = authService;
        
            _userRepository = userRepository;

        }    

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var validacion = new UserValidation();
            await validacion.ValidateRequest(request.Users);
               var newUser = new User(
                request.Users.UserNombre,
                request.Users.UserApellido,
                request.Users.UserCorreo,
                request.Users.UserTelefono,
                request.Users.UserDireccion,
                request.Users.UserPassword
            );
            
          await _userRepository.CreateAsync(newUser);

            return newUser.UserId;

        }


    }
} 