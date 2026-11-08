using System.Threading;
using System.Threading.Tasks;

public interface IPlayerAccountsLogin
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    void SignOut();
}
