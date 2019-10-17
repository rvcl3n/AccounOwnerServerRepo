using System;
using System.Collections.Generic;
using Contracts;
using Entities.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace UserService
{
    public class UserManager : IUserService
    {
        // users hardcoded for simplicity, store in a db with hashed passwords in production applications
        private List<Owner> _users = new List<Owner>
        {
            new Owner { Id = new Guid(), Name = "TestAuth", Email = "testEmail", Password = "test", DateOfBirth=new DateTime(), Address ="TestAddress"}
        };

        private readonly AppSettings _appSettings;

        public UserManager(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public Owner Authenticate(string username, string password)
        {
            var user = _users.SingleOrDefault(x => x.Email == username && x.Password == password);

            // return null if user not found
            if (user == null)
                return null;

            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);

            // remove password before returning
            user.Password = null;

            return user;
        }
    }
}
