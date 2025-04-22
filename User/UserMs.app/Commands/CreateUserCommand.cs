using UserMs.app.DTO.Request;
using UserMs.Domain.Entities;
using MediatR;

namespace UserMs.app.Commands
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