using MediatR;
using Users.Domain.Entities;
using Users.Application.DTO.Respond;

namespace Users.Application.Queries
{
    public class GetUserByIdQuery : IRequest<GetUserDTO>
    {
        public Guid UserId { get; set; }

        public GetUserByIdQuery(Guid id)
        {
            UserId = id; 
        }
    }
}