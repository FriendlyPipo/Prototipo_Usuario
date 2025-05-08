using Users.Application.DTO.Request;
using Users.Domain.Entities;
using MediatR;

namespace Users.Application.Commands
{
    public class DeleteUserCommand : IRequest<string>   
    {
        public DeleteUserDTO Users { get; set; }

        public DeleteUserCommand( DeleteUserDTO users)
        {
            Users = users;
        }
    }
}