using System.Threading.Tasks;

public interface IGameAuthentication
{
    string PlayerId { get; }
    bool IsSignedIn { get; }
    bool IsAuthorized { get; }
    bool HasUnityIdentity { get; }
    bool HasCachedSessionToken { get; }
    Task InitializeAsync();
    Task SignInCachedOrGuestAsync();
    Task SignInUnityAsync(string accessToken, bool createAccount);
    Task RefreshPlayerIdentityAsync();
    Task LinkToUnityAsync(string playerAccountsAccessToken);
    Task SignInExistingUnityAsync(string playerAccountsAccessToken);
    void SignOut(bool clearSessionToken);
}
