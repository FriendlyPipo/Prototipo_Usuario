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
    using Users.Core.DTO;
    using System.Text.Json;

    namespace Users.Application.Handlers
    {
        public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, string>
        {
            private readonly IUserRepository _userRepository;
            private readonly UserDbContext _dbContext;
            private readonly IKeycloakRepository _keycloakRepository;

            public CreateUserCommandHandler(IUserRepository userRepository, UserDbContext dbContext, IKeycloakRepository keycloakRepository /*, IAuthService authService*/)
            {
                _userRepository = userRepository;
                _dbContext = dbContext;
                _keycloakRepository = keycloakRepository;
                /* _authService = authService;  */
            }

            public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
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


                    // Crear usuario en Keycloak
            var token = await _keycloakRepository.GetTokenAsync();
            var KcUser = new KcCreateUserDTO
            {
                username = request.Users.UserEmail,
                email = request.Users.UserEmail,
                enabled = true,
                firstName = request.Users.UserName,
                lastName = request.Users.UserLastName,  
                credentials = new [] { new { type = "password", value = request.Users.UserPassword, temporary = false } },
            };

            var createUserResponseJson = await _keycloakRepository.CreateUserAsync(KcUser, token);
                return "Usuario Registrado Correctamente";
                 
            }
        }
    }