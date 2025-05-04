using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Users.Core.DTO
{
    public class KcCreateUserDTO
    {
        public string username { get; set; } = String.Empty;
        public object credentials { get; set; } = new object();

    }
}