using UserMs.Domain.Entities;

namespace UserMs.app.DTO.Request
{
    public record CreateUserDTO
    {   
        public required string UserNombre { get; set; }
        public required string UserApellido { get; set;}
        public required string UserCorreo { get; set; }
        public required string UserTelefono { get; set; }
        public required string UserDireccion { get; set; }
    }
}