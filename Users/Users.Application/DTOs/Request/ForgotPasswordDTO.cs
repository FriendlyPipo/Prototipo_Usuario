namespace Users.Application.DTO.Request
{
    public record ForgotPasswordDTO
    {
        public required string UserEmail { get; set; } = null!;
    }
}