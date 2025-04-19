using UserMs.app.DTO.Request;
using UserMs.Domain.Entities;
using MediatR;

namespace UserMs.app.Commands
{
    public class CreateUserCommand : IRequest<Guid>   
    {
        public CreateUserDTO User { get; set; }

        public CreateUserCommand(CreateUserDTO user)
        {
            User = user;
        }
    }
}