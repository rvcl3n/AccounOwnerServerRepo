using Entities.Models;

namespace Contracts
{
    public interface IUserService
    {
        Owner Authenticate(string username, string password);
    }
}
