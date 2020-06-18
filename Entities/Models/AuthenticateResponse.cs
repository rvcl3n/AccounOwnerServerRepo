using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Models
{
    public class AuthenticateResponse
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Token { get; set; }

        public string RefreshToken { get; set; }

        public AuthenticateResponse(Owner owner, string jwtToken, string refreshToken)
        {
            Id = owner.Id.ToString();
            Name = owner.Name;
            Token = jwtToken;
            RefreshToken = refreshToken;
        }
    }
}
