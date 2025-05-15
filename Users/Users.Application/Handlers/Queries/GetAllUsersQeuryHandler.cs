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
        private readonly IUserReadRepository _userReadRepository;

        public GetAllUsersQueryHandler(IUserReadRepository userReadRepository)
        {
            _userReadRepository = userReadRepository;
        }

        public async Task<List<GetUserDTO>> Handle(GetAllUsesrQuery request, CancellationToken cancellationToken)
        {
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
    }
}