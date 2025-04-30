using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KcAuthentication.Common.Dtos
{
    public class TokenDto
    {
        public string RefreshToken { get; set; } = String.Empty;
        public string AuthenticationToken { get; set; } = String.Empty;
    }
}