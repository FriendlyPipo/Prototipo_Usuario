namespace Users.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; private set; }
        public string UserName { get; private set; }
        public string UserLastName { get; private set; }
        public string UserEmail { get; private set; }
        public string UserPhoneNumber { get; private set; }
        public string UserDirection { get; private set; }
        public DateTime createdAt { get; private set; }
        public bool UserConfirmation { get; private set; }
        public string UserPassword { get; private set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public User(string UserName, string UserLastName, string UserEmail, string UserPhoneNumber, string UserDirection, string UserPassword)
        {
            UserId = Guid.NewGuid();
            this.UserName = UserName;
            this.UserLastName = UserLastName;
            this.UserEmail = UserEmail;
            this.UserPhoneNumber = UserPhoneNumber;
            this.UserDirection = UserDirection;
            this.UserPassword = UserPassword;
            this.UserConfirmation = false;
            this.createdAt = DateTime.UtcNow;
        }
    }
}