using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

/// <summary>All direct Unity Authentication SDK calls live here.</summary>
public sealed class UgsAuthenticationGateway : IGameAuthentication
{
    private bool _hasUnityIdentity;

    public string PlayerId => AuthenticationService.Instance.PlayerId;
    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
    public bool IsAuthorized => AuthenticationService.Instance.IsAuthorized;
    public bool HasUnityIdentity => _hasUnityIdentity;
    public bool HasCachedSessionToken => AuthenticationService.Instance.SessionTokenExists;

    public Task InitializeAsync() => UnityServices.InitializeAsync();

    public async Task SignInCachedOrGuestAsync()
    {
        bool wasCached = AuthenticationService.Instance.SessionTokenExists;
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        if (!wasCached) _hasUnityIdentity = false;
    }

    public async Task RefreshPlayerIdentityAsync()
    {
        var info = await AuthenticationService.Instance.GetPlayerInfoAsync();
        _hasUnityIdentity = info != null && info.GetUnityId() != null;
    }

    public async Task LinkToUnityAsync(string accessToken)
    {
        await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
        _hasUnityIdentity = true;
    }

    public Task SignInExistingUnityAsync(string accessToken) =>
        SignInUnityAsync(accessToken, createAccount: false);

    public async Task SignInUnityAsync(string accessToken, bool createAccount)
    {
        await AuthenticationService.Instance.SignInWithUnityAsync(
            accessToken, new SignInOptions { CreateAccount = createAccount });
        _hasUnityIdentity = true;
    }

    public void SignOut(bool clearSessionToken)
    {
        var auth = AuthenticationService.Instance;
        if (auth.IsSignedIn)
            auth.SignOut(clearSessionToken);
        else if (clearSessionToken && auth.SessionTokenExists)
            auth.ClearSessionToken();
        _hasUnityIdentity = false;
    }
}