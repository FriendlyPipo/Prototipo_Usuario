namespace UserMs.app.DTO.Respond
{
    public record GetUserDTO
    {
        public Guid UserId { get; init; } // Usamos init para que solo se pueda establecer al crear el objeto
        public string? UserNombre { get; init; }
        public string? UserApellido { get; init; }
        public string? UserCorreo { get; init; }
        public string? UserTelefono { get; init; }
        public string? UserDireccion { get; init; }
        public string? UserRol { get; init; } // Devolvemos el rol como string para la respuesta
    }
}