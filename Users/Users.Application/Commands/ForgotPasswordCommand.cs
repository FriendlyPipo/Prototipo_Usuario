using Users.Application.DTO.Request;
using Users.Domain.Entities;
using MediatR;

namespace Users.Application.Commands
{
    public class ForgotPasswordCommand : IRequest<string>   
    {
        public ForgotPasswordDTO Users { get; set; }

        public ForgotPasswordCommand(ForgotPasswordDTO user)
        {
            Users = user;
        }
    }
}