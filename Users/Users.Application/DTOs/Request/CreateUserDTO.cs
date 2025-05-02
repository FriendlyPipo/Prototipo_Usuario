
namespace Users.Application.DTO.Request
{
    public record CreateUserDTO
    {   
        public required string UserName { get; set; }
        public required string UserLastName { get; set;}
        public required string UserEmail { get; set; }
        public required string UserPhoneNumber { get; set; }
        public required string UserDirection { get; set; }
        public required string UserRole { get; set; }
        public required string UserPassword { get; set; } = string.Empty;
        public required bool UserConfirmation { get; set; }
    }
}   