namespace Users.Application.DTO.Request
{
    public record DeleteUserDTO
    {
        public required Guid UserId { get; set; }
    }
}