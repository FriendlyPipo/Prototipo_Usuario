using Users.Application.DTO.Respond;
using Users.Application.Queries;
using Users.Core.Repositories;
using Users.Infrastructure.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Users.Application.Handlers.Queries
{
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsesrQuery, List<GetUserDTO>>
    {
        private readonly IUserRepository _userRepository;

        public GetAllUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<GetUserDTO>> Handle(GetAllUsesrQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllAsync();

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
                UserRole = user.UserRoles.FirstOrDefault()?.RoleName.ToString()
            }).ToList();
        }
    }
}