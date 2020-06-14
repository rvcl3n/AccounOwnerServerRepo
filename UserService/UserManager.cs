using System;
using System.Collections.Generic;
using Contracts;
using Entities.Models;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace UserService
{
    public class UserManager : IUserService
    {
        private IRepositoryWrapper _repository;
        private readonly AppSettings _appSettings;

        public UserManager(IRepositoryWrapper repository, IOptions<AppSettings> appSettings)
        {
            _repository = repository;
            _appSettings = appSettings.Value;
        }

        public IEnumerable<Owner> GetAll()
        {
            return _repository.Owner.GetAllOwners();
        }

        public Owner GetById(Guid id)
        {
            return _repository.Owner.GetOwnerById(id);
        }

        public Owner Create(Owner owner, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
            { throw new Exception("Password is required"); }

            if (_repository.Owner.GetOwnerByEmail(owner.Email) != null)
            { throw new Exception("Username \"" + owner.Email + "\" is already taken"); }

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            owner.PasswordHash = passwordHash;
            owner.PasswordSalt = passwordSalt;

            _repository.Owner.CreateOwner(owner);
            _repository.Save();

            return owner;
        }

        public Owner Authenticate(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return null;

            var user = _repository.Owner.GetOwnerByEmail(email);

            // check if username exists
            if (user == null)
                return null;

            // check if password is correct
            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                return null;

            // authentication successful
            return user;
        }

        public string GetJWTToken(Guid userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, userId.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public void Update(Owner ownerParam, string password = null)
        {
            var owner = _repository.Owner.GetOwnerById(ownerParam.Id);

            if (owner == null)
                throw new Exception("User not found");

            if (ownerParam.Email != owner.Email)
            {
                // username has changed so check if the new username is already taken
                if (_repository.Owner.GetOwnerByEmail(ownerParam.Email) != null)
                    throw new Exception("Username " + ownerParam.Email + " is already taken");
            }

            // update user properties
            owner.Email = ownerParam.Email;
            owner.Name = ownerParam.Name;
            owner.DateOfBirth = ownerParam.DateOfBirth;
            owner.Address = ownerParam.Address;

            // update password if it was entered
            if (!string.IsNullOrWhiteSpace(password))
            {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                owner.PasswordHash = passwordHash;
                owner.PasswordSalt = passwordSalt;
            }

            _repository.Owner.Update(owner);
            _repository.Save();
        }

        public void Delete(Owner ownerParam)
        {
            var owner = _repository.Owner.GetOwnerById(ownerParam.Id);
            if (owner != null)
            {
                _repository.Owner.DeleteOwner(owner);
                _repository.Save();
            }
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }
    }
}
