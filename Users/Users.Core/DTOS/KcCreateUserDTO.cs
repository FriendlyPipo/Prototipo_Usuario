using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Users.Core.DTO
{
    public class KcCreateUserDTO
    {
        public string username { get; set; } = String.Empty;
        public string email { get; set; } = String.Empty;
        public bool enabled { get; set; }
        public string firstName { get; set; } = String.Empty;
        public string lastName { get; set; } = String.Empty;
        public object[] credentials { get; set; }

    }
}