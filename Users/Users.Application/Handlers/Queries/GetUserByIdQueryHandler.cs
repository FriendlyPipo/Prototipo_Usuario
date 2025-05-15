using Users.Application.Queries;
using Users.Core.Repositories;
using Users.Application.DTO.Respond;
using Users.Infrastructure.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Users.Application.Handlers.Queries
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, GetUserDTO>
    {
        private readonly IUserReadRepository _userReadRepository;

        public GetUserByIdQueryHandler(IUserReadRepository userReadRepository)
        {
            _userReadRepository = userReadRepository;
        }   

        public async Task<GetUserDTO> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
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
    }
}