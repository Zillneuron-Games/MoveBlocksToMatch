using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Zillneuron.Utilities;

#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#elif UNITY_IOS && !UNITY_EDITOR
using System.Text;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
#endif

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>Authenticates the device's platform account with Unity Services.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp")]
    public class SignInService : MonoSingleton<SignInService>
    {
        private static readonly TimeSpan ProviderTimeout = TimeSpan.FromMinutes(2);

#if UNITY_IOS && !UNITY_EDITOR
        private IAppleAuthManager appleAuth;

        private void Update()
        {
            appleAuth?.Update();
        }
#endif

        public bool IsAuthorized
        {
            get
            {
                return UnityServices.State == ServicesInitializationState.Initialized &&
                    AuthenticationService.Instance.IsAuthorized;
            }
        }

        public string PlayerId
        {
            get
            {
                return IsAuthorized ? AuthenticationService.Instance.PlayerId : null;
            }
        }

        public async Task RestoreSessionAsync(string expectedPlayerId)
        {
            await UnityServices.InitializeAsync();
            IAuthenticationService authentication = AuthenticationService.Instance;
            if (!authentication.IsAuthorized && authentication.SessionTokenExists)
            {
                if (authentication.IsSignedIn)
                {
                    authentication.SignOut();
                }

                // Unity uses this API to resume an existing cached session.
                await authentication.SignInAnonymouslyAsync(new SignInOptions { CreateAccount = false });
            }

            if (authentication.IsAuthorized &&
                !string.Equals(authentication.PlayerId, expectedPlayerId, StringComparison.Ordinal))
            {
                SignOut();
            }
        }

        /// <summary>Returns whether the platform identity needed a new Blockmates account.</summary>
        public async Task<bool> SignInAsync()
        {
            await UnityServices.InitializeAsync();
            SignOut();

            string credential = await GetCredentialAsync();
            try
            {
                await AuthenticateAsync(credential, createAccount: false);
                return false;
            }
            catch (RequestFailedException error) when (error.ErrorCode == CommonErrorCodes.NotFound)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Google authorization codes are single-use.
                credential = await GetCredentialAsync();
#endif
                await AuthenticateAsync(credential, createAccount: true);
                return true;
            }
        }

        public void SignOut()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                return;
            }

            IAuthenticationService authentication = AuthenticationService.Instance;
            if (authentication.IsSignedIn)
            {
                authentication.SignOut(clearCredentials: true);
            }
            else if (authentication.SessionTokenExists)
            {
                authentication.ClearSessionToken();
            }
        }

        private static Task AuthenticateAsync(string credential, bool createAccount)
        {
            var options = new SignInOptions { CreateAccount = createAccount };
#if UNITY_ANDROID && !UNITY_EDITOR
            return AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(credential, options);
#elif UNITY_IOS && !UNITY_EDITOR
            return AuthenticationService.Instance.SignInWithAppleAsync(credential, options);
#else
            throw new PlatformNotSupportedException("Sign-in is available on Android and iOS devices.");
#endif
        }

        private async Task<string> GetCredentialAsync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            PlayGamesSettings settings = PlayGamesSettings.LoadInstance();
            if (settings == null || string.IsNullOrEmpty(settings.WebClientId))
            {
                throw new InvalidOperationException("Google Play Games sign-in is not configured.");
            }

            PlayGamesPlatform platform = PlayGamesPlatform.Instance;
            if (!platform.IsAuthenticated())
            {
                var signIn = new TaskCompletionSource<SignInStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
                platform.ManuallyAuthenticate(status => signIn.TrySetResult(status));
                SignInStatus result = await WithTimeout(signIn.Task);
                if (result == SignInStatus.Canceled)
                {
                    throw new OperationCanceledException("Sign-in was cancelled.");
                }

                if (result != SignInStatus.Success)
                {
                    throw new InvalidOperationException("Could not sign in to Google Play Games.");
                }
            }

            var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            platform.RequestServerSideAccess(false, code => response.TrySetResult(code));
            string credential = await WithTimeout(response.Task);
#elif UNITY_IOS && !UNITY_EDITOR
            if (!AppleAuthManager.IsCurrentPlatformSupported)
            {
                throw new PlatformNotSupportedException("Sign in with Apple is unavailable on this device.");
            }

            appleAuth ??= new AppleAuthManager(new PayloadDeserializer());
            var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            appleAuth.LoginWithAppleId(new AppleAuthLoginArgs(LoginOptions.None), credential =>
            {
                byte[] token = (credential as IAppleIDCredential)?.IdentityToken;
                response.TrySetResult(token == null ? null : Encoding.UTF8.GetString(token));
            }, error =>
            {
                if (error.GetAuthorizationErrorCode() == AuthorizationErrorCode.Canceled)
                {
                    response.TrySetCanceled();
                }
                else
                {
                    response.TrySetException(new InvalidOperationException("Could not sign in with Apple."));
                }
            });
            string credential = await WithTimeout(response.Task);
#else
            await Task.CompletedTask;
            throw new PlatformNotSupportedException("Sign-in is available on Android and iOS devices.");
#endif
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (string.IsNullOrEmpty(credential))
            {
                throw new InvalidOperationException("The sign-in provider did not return a credential.");
            }

            return credential;
#endif
        }

        private static async Task<T> WithTimeout<T>(Task<T> task)
        {
            if (await Task.WhenAny(task, Task.Delay(ProviderTimeout)) != task)
            {
                throw new TimeoutException("The sign-in prompt timed out.");
            }

            return await task;
        }
    }
}
