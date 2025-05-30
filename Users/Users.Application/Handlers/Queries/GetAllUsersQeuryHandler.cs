using Users.Application.DTO.Respond;
using Users.Application.Queries;
using Users.Core.Repositories;
using Users.Infrastructure.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Users.Application.Handlers.Queries
{
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<GetUserDTO>>
    {
        private readonly IUserReadRepository _userReadRepository;
        private readonly ILogger<GetAllUsersQueryHandler> _logger;

        public GetAllUsersQueryHandler(IUserReadRepository userReadRepository, ILogger<GetAllUsersQueryHandler> logger)
        {
            _userReadRepository = userReadRepository;
            _logger = logger;
        }

        public async Task<List<GetUserDTO>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {   
            try{
                var users = await _userReadRepository.GetAllAsync();

                if (users == null || !users.Any())
                {
                    throw new UserNotFoundException("No se encontraron usuarios");
                }

                return users.Select(user => new GetUserDTO
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    UserLastName = user.UserLastName,
                    UserEmail = user.UserEmail,
                    UserPhoneNumber = user.UserPhoneNumber,
                    UserDirection = user.UserDirection,
                    UserRole = user.UserRole
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los usuarios");
                throw new UserException("Error al obtener todos los usuarios", ex);
            }
        }
    }
}