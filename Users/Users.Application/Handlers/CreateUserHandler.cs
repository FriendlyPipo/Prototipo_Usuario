// Por probar 
using MediatR;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Application.Commands;
using Users.Infrastructure.Data;
using Users.Domain.Interfaces;
using Users.Infrastructure.Exceptions;
using Users.Infrastructure.Interfaces;
using Users.Application.UserValidations;

namespace Users.Application.Handlers
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
                request.Users.UserName,
                request.Users.UserLastName,
                request.Users.UserEmail,
                request.Users.UserPhoneNumber,
                request.Users.UserDirection,
                request.Users.UserPassword
            );
            
          await _userRepository.CreateAsync(newUser);

            return newUser.UserId;

        }


    }
} 