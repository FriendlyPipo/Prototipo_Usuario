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

namespace Users.Application.Handlers
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly UserDbContext _dbContext;

        public CreateUserCommandHandler(IUserRepository userRepository, UserDbContext dbContext /*, IAuthService authService*/)
        {
            _userRepository = userRepository;
            _dbContext = dbContext;
            /* _authService = authService;  */
        }

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            try
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
                    }
                    else
                    {
                        Console.WriteLine($"El valor del rol '{request.Users.UserRole}' no es válido.");
                    }
                }

                await _userRepository.CreateAsync(newUser); 
                await _dbContext.SaveChangesAsync(); 

                return newUser.UserId;
            }
            catch (Exception)
            {
                throw;
            }   
        }
    }
}