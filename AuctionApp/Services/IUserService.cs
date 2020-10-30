using System.Collections.Generic;
using System.Threading.Tasks;
using SignalWithRedisDemoApp.Models;

namespace SignalWithRedisDemoApp.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> List();
    }
}
