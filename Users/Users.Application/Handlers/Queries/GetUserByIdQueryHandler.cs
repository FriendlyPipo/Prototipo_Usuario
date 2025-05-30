using Users.Application.Queries;
using Users.Core.Repositories;
using Users.Application.DTO.Respond;
using Users.Infrastructure.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Users.Application.Handlers.Queries
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, GetUserDTO>
    {
        private readonly IUserReadRepository _userReadRepository;
        private readonly ILogger<GetUserByIdQueryHandler> _logger;

        public GetUserByIdQueryHandler(IUserReadRepository userReadRepository, ILogger<GetUserByIdQueryHandler> logger)
        {
            _userReadRepository = userReadRepository;
            _logger = logger;
        }   

        public async Task<GetUserDTO> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
            var user = await _userReadRepository.GetByIdAsync(request.UserId);

            if (user == null)
            {
                throw new UserNotFoundException("Usuario no encontrado");
            }

            return new GetUserDTO
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserLastName = user.UserLastName,
                UserEmail = user.UserEmail,
                UserPhoneNumber = user.UserPhoneNumber,
                UserDirection = user.UserDirection,
                UserRole = user.UserRole
            };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por ID");
                throw new UserException("Error al obtener usuario por ID", ex);
            }
        }
    }
}