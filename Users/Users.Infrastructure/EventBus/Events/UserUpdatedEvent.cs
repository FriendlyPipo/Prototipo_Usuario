namespace Users.Infrastructure.EventBus.Events
{
    public record UserUpdatedEvent(
        Guid UserId,
        string? UserName, 
        string? UserLastName, 
        string? UserEmail, 
        string? UserPhoneNumber, 
        string? UserDirection, 
        DateTime CreatedAt, 
        string? CreatedBy, 
        DateTime? UpdatedAt, 
        string? UpdatedBy, 
        bool? UserConfirmation, 
        string? UserPassword,
        Guid RoleId,
        string? RoleName); 
}