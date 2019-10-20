using Entities.Models;
using System;

namespace Contracts
{
    public interface IUserService
    {
        Owner Authenticate(string username, string password);

        Owner Create(Owner owner, string password);

        Owner GetById(Guid id);
    }
}
