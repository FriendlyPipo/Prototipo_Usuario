namespace Users.Application.DTO.Respond
{
    public record GetUserDTO
    {
        public Guid UserId { get; init; } // Usamos init para que solo se pueda establecer al crear el objeto
        public string? UserName { get; init; }
        public string? UserLastName { get; init; }
        public string? UserEmail { get; init; }
        public string? UserPhoneNumber { get; init; }
        public string? UserDirection { get; init; }
        public string? UserRole { get; init; } // Devolvemos el rol como string para la respuesta
    }
}