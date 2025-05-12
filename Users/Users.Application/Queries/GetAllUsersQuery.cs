
using MediatR;
using Users.Application.DTO.Respond;

namespace Users.Application.Queries
{
    public class GetAllUsesrQuery : IRequest<List<GetUserDTO>>
    {
        public Guid UserId { get; set; } 
        public string? UserName { get; set; }
        public string? UserLastName { get; set; }
        public string? UserEmail { get; set; } 
        public string? UserPhoneNumber { get; set; }
        public string? UserDirection { get; set; }
        public string? UserRol { get; set; } // Devolvemos el rol como string para la respuesta
    }
}
