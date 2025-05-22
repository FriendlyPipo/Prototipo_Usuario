namespace Users.Infrastructure.EventBus.Events
{
    public record UserCreatedEvent(
        Guid UserId,
        string UserName,
        string UserLastName,
        string UserEmail,
        string UserPhoneNumber,
        string UserDirection,
        DateTime CreatedAt,
        string? CreatedBy,
        DateTime? UpdatedAt,
        string? UpdatedBy,
        Guid RoleId,
        string RoleName); 
}