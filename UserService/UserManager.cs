using System;
using System.Collections.Generic;
using Contracts;
using Entities.Models;
using System.Text;

namespace UserService
{
    public class UserManager : IUserService
    {
        private IRepositoryWrapper _repository;

        public UserManager(IRepositoryWrapper repository)
        {
            _repository = repository;
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
