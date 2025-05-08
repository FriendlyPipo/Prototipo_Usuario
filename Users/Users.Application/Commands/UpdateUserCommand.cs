using Users.Application.DTO.Request;
using Users.Domain.Entities;
using MediatR;

namespace Users.Application.Commands
{
    public class UpdateUserCommand : IRequest<string>   
    {
        public UpdateUserDTO Users { get; set; }

        public UpdateUserCommand(UpdateUserDTO user)
        {
            Users = user;
        }
    }
}