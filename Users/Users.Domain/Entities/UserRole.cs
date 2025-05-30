namespace Users.Domain.Entities
{
    public enum UserRoleName
    {
        Administrador,
        Soporte,
        Postor,
        Subastador
    }

    public class UserRole
    {
        public Guid RoleId { get; private set; }
        public UserRoleName RoleName { get; private set; }
        public Guid UserId { get; set; }
        public User User { get; set; }

        public UserRole(UserRoleName roleName)
        {
            RoleId = Guid.NewGuid();
            this.RoleName = roleName;   
        }

        public UserRole() { }
    }
}