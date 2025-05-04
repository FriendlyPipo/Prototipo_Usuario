using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Users.Core.DTO
{
    public class KcTokenDTO
    {
        public string RefreshToken { get; set; } = String.Empty;
        public string AuthenticationToken { get; set; } = String.Empty;
    }
}