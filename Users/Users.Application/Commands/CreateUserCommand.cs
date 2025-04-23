using Users.Application.DTO.Request;
using Users.Domain.Entities;
using MediatR;

namespace Users.Application.Commands
{
    public class CreateUserCommand : IRequest<Guid>   
    {
        public CreateUserDTO Users { get; set; }

        public CreateUserCommand(CreateUserDTO user)
        {
            Users = user;
        }
    }
}