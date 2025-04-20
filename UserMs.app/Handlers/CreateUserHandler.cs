using MediatR;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserMs.Domain.Entities;
using UserMs.app.Commands;
using UserMs.Infra.Data;
using UserMs.Domain.Interfaces;
using UserMs.Domain.Exceptions;


namespace UserMs.app.Handlers
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand,Guid>
    {
        private readonly IUserRepository _userRepository;

        public CreateUserHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }    

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            
        }


    }



}