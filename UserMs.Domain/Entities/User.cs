namespace UserMs.Domain.Entities{
    public class User {
        public Guid UserId { get; private set; }    
        public string UserNombre { get; private set; } 
        public string UserApellido { get; private set; }
        public string UserCorreo { get; private set; }   
        public string UserTelefono { get; private set; }
        public string UserDireccion { get; private set; }
        public DateTime createdAt { get; private set; }
        public bool UserConfirmacion { get; private set; }
        public string UserPassword { get; private set; }
        public UserRolEnum UserRol { get; private set; }
    public User(string UserNombre, string UserApellido, string UserCorreo, string UserTelefono, string UserDireccion, string UserPassword)
        {
            UserId = Guid.NewGuid();
            this.UserNombre = UserNombre;
            this.UserApellido = UserApellido;
            this.UserCorreo = UserCorreo;
            this.UserTelefono = UserTelefono;
            this.UserDireccion = UserDireccion;
            this.UserPassword = UserPassword;
            this.UserConfirmacion = false;
            this.createdAt = DateTime.UtcNow;
        }

        public string RolToString()
        {
            return UserRol.ToString();
        }
        public void SetUserRol(UserRolEnum userRol)
        {
            UserRol = userRol;
        }
    }
}