namespace UserMs.Domain.Entities{

    public class User {
        public Guid UserId { get; set; }    
        public string UserNombre { get; set; } 
        public string UserApellido { get; set; }
        public string UserCorreo { get; set; }   
        public string UserTelefono { get; set; }
        public string UserDireccion { get; set; }
        public DateTime createdAt { get; set; }

               public User(string UserNombre, string UserApellido, string UserCorreo, string UserTelefono, string UserDireccion)
        {
            UserId = Guid.NewGuid();
            this.UserNombre = UserNombre;
            this.UserApellido = UserApellido;
            this.UserCorreo = UserCorreo;
            this.UserTelefono = UserTelefono;
            this.UserDireccion = UserDireccion;
        }

    }



}