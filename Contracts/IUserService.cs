using Entities.Models;
using System;

namespace Contracts
{
    public interface IUserService
    {
        Owner Authenticate(string username, string password);

        string GetJWTToken(Guid userId);

        string GetJWTRefreshToken(string ipAddress);

        AuthenticateResponse RefreshToken(string token, string ipAddress);

        Owner Create(Owner owner, string password);

        Owner GetById(Guid id);

        void Update(Owner user, string password = null);

        void Delete(Owner ownerParam);
    }
}
