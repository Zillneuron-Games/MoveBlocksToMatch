using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>All direct Unity Authentication SDK calls live here.</summary>
    public sealed class UgsAuthenticationGateway : IGameAuthentication
    {
        private bool hasUnityIdentity;

        public string PlayerId
        {
            get
            {
                return AuthenticationService.Instance.PlayerId;
            }
        }

        public bool IsSignedIn
        {
            get
            {
                return AuthenticationService.Instance.IsSignedIn;
            }
        }

        public bool IsAuthorized
        {
            get
            {
                return AuthenticationService.Instance.IsAuthorized;
            }
        }

        public bool HasUnityIdentity
        {
            get
            {
                return hasUnityIdentity;
            }
        }

        public bool HasCachedSessionToken
        {
            get
            {
                return AuthenticationService.Instance.SessionTokenExists;
            }
        }

        public Task InitializeAsync()
        {
            return UnityServices.InitializeAsync();
        }

        public async Task SignInCachedOrGuestAsync()
        {
            bool wasCached = AuthenticationService.Instance.SessionTokenExists;
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            if (!wasCached)
            {
                hasUnityIdentity = false;
            }
        }

        public async Task RefreshPlayerIdentityAsync()
        {
            var info = await AuthenticationService.Instance.GetPlayerInfoAsync();
            hasUnityIdentity = info != null && info.GetUnityId() != null;
        }

        public async Task LinkToUnityAsync(string accessToken)
        {
            await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
            hasUnityIdentity = true;
        }

        public Task SignInExistingUnityAsync(string accessToken)
        {
            return SignInUnityAsync(accessToken, createAccount: false);
        }

        public async Task SignInUnityAsync(string accessToken, bool createAccount)
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken, new SignInOptions { CreateAccount = createAccount });
            hasUnityIdentity = true;
        }

        public void SignOut(bool clearSessionToken)
        {
            var auth = AuthenticationService.Instance;
            if (auth.IsSignedIn)
            {
                auth.SignOut(clearSessionToken);
            }
            else if (clearSessionToken && auth.SessionTokenExists)
            {
                auth.ClearSessionToken();
            }

            hasUnityIdentity = false;
        }
    }
}
