using InventorySystem.Core.Models;

namespace InventorySystem.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
}
