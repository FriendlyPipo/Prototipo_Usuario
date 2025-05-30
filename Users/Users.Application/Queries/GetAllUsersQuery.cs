
using MediatR;
using Users.Application.DTO.Respond;

namespace Users.Application.Queries
{
    public class GetAllUsersQuery : IRequest<List<GetUserDTO>>
    {
    }
}
