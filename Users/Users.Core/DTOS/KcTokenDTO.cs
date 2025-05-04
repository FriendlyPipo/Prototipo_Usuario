using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Users.Core.DTO
{
    public class KcTokenDTO
    {
        public string refresh_token { get; set; } = String.Empty;
        public string access_token { get; set; } = String.Empty;
    }
}