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
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; } 
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public User(string UserName, string UserLastName, string UserEmail, string UserPhoneNumber, string UserDirection)
        {
            UserId = Guid.NewGuid();
            this.UserName = UserName;
            this.UserLastName = UserLastName;
            this.UserEmail = UserEmail;
            this.UserPhoneNumber = UserPhoneNumber;
            this.UserDirection = UserDirection;
            this.CreatedAt = DateTime.UtcNow;
        }

         public void UpdateUserName(string userName)
        {
            UserName = userName;
        }

        public void UpdateUserLastName(string userLastName)
        {
            UserLastName = userLastName;
        }

        public void UpdateUserEmail(string userEmail)
        {
            UserEmail = userEmail;
        }

        public void UpdateUserPhoneNumber(string userPhoneNumber)
        {
            UserPhoneNumber = userPhoneNumber;
        }

        public void UpdateUserDirection(string userDirection)
        {
            UserDirection = userDirection;
        }

    }
}