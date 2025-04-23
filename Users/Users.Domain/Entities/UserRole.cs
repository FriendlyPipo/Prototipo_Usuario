namespace Users.Domain.Entities
{
  public class UserRole
    {
        public int RoleId { get; private set; }
        public string RoleName { get; private set; }

        public ICollection<User> Users { get; set; }

        public UserRole(string RoleName)
        {
            this.RoleName = RoleName;
        }

        public UserRole()
        {
            Users = new List<User>();
        }
    }
}