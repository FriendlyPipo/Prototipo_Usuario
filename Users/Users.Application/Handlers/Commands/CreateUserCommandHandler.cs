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
        public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, string>
        {
            private readonly IUserWriteRepository _userWriteRepository;
            private readonly UserDbContext _dbContext;
            private readonly IKeycloakRepository _keycloakRepository;
            private readonly IEventBus _eventBus;

            public CreateUserCommandHandler(IEventBus eventBus,IUserWriteRepository userWriteRepository, UserDbContext dbContext, IKeycloakRepository keycloakRepository)
            {
                _userWriteRepository = userWriteRepository;
                _dbContext = dbContext;
                _keycloakRepository = keycloakRepository;
                _eventBus = eventBus;
            }

            public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
            {
                    var validacion = new UserValidation();
                    Guid roleId= Guid.Empty;
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
                            roleId = newUserRole.RoleId;
                        }
                        else
                        {
                            Console.WriteLine($"El valor del rol '{request.Users.UserRole}' no es válido.");
                        }
                    }
    
                    await _userWriteRepository.CreateAsync(newUser);   
                    await _dbContext.SaveChangesAsync();

                        //Registrar en MongoDB
                var userCreatedEvent = new UserCreatedEvent(newUser.UserId, newUser.UserName, newUser.UserLastName, newUser.UserEmail, newUser.UserPhoneNumber, newUser.UserDirection, newUser.CreatedAt, newUser.CreatedBy, newUser.UpdatedAt, newUser.UpdatedBy, newUser.UserConfirmation, newUser.UserPassword,roleId, request.Users.UserRole);
                _eventBus.Publish<UserCreatedEvent>(userCreatedEvent, "user.created");   

                    // Crear usuario en Keycloak
            var token = await _keycloakRepository.GetTokenAsync();
            var KcUser = new 
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