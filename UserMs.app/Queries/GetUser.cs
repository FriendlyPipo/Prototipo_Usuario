
using MediatR;
using UserMs.app.DTO.Respond;

namespace UserMs.app.Queries
{
    public class GetUserQuery : IRequest<GetUserDTO>
    {
        public Guid UserId { get; set; } 
        public string? UserNombre { get; set; }
        public string? UserApellido { get; set; }
        public string? UserCorreo { get; set; } 
        public string? UserTelefono { get; set; }
        public string? UserDireccion { get; set; }
        public string? UserRol { get; set; } // Devolvemos el rol como string para la respuesta
    }
}
