using System.Security.Principal;

namespace Pitstop.Services
{
    public interface IIdentityParser<T>
    {
        T Parse(IPrincipal principal);
    }
}
