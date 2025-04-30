using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KcAuthentication.Common.Dtos
{
    public class CreateUserDto
    {
        public string UserEmail { get; set; } = String.Empty;
        public string UserPassword { get; set; } = String.Empty;

    }
}