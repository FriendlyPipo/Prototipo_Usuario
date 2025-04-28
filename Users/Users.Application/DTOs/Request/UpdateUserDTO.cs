namespace Users.Application.DTO.Request
{
    public record UpdateUserDTO
    {
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserLastName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhoneNumber { get; set; }
        public string? UserDirection { get; set; }
        public string? UserPassword { get; set; }
    }
}