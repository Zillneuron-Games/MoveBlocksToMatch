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

    public Task InitializeAsync() => UnityServices.InitializeAsync();

    public Task SignInCachedOrGuestAsync() =>
        AuthenticationService.Instance.SignInAnonymouslyAsync();

    public async Task RefreshPlayerIdentityAsync()
    {
        var info = await AuthenticationService.Instance.GetPlayerInfoAsync();
        _hasUnityIdentity = info != null && info.GetUnityId() != null;
    }

    public Task LinkToUnityAsync(string accessToken) =>
        AuthenticationService.Instance.LinkWithUnityAsync(accessToken);

    public Task SignInExistingUnityAsync(string accessToken) =>
        AuthenticationService.Instance.SignInWithUnityAsync(
            accessToken, new SignInOptions { CreateAccount = false });

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