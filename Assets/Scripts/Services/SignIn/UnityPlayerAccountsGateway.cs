using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;

namespace Zillneuron.Blockmates.Services.SignIn
{
    /// <summary>
    /// Browser login may finish AFTER StartSignInAsync returns.
    /// Listen for SignedIn and SignInFailed; cancel/timeout if no callback arrives.
    /// </summary>
    public sealed class UnityPlayerAccountsGateway : IPlayerAccountsLogin
    {
        private static readonly TimeSpan BrowserTimeout = TimeSpan.FromMinutes(5);

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            var accounts = PlayerAccountService.Instance;

            if (accounts.IsSignedIn)
            {
                accounts.SignOut(); // Don't silently reuse the previous provider token.
            }

            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            void SignedIn()
            {
                completion.TrySetResult(accounts.AccessToken);
            }

            void Failed(RequestFailedException error)
            {
                completion.TrySetException(error);
            }

            accounts.SignedIn += SignedIn;
            accounts.SignInFailed += Failed;
            bool success = false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await accounts.StartSignInAsync();

                // Browser close may not raise SignInFailed. Bound the wait.
                Task timeout = Task.Delay(BrowserTimeout, cancellationToken);
                Task first = await Task.WhenAny(completion.Task, timeout);
                
                if (first != completion.Task)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException("Unity Player Accounts sign-in timed out or was cancelled.");
                }

                string accessToken = await completion.Task;
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new InvalidOperationException("Player Accounts returned no access token.");
                }

                success = true;
                return accessToken;
            }
            finally
            {
                accounts.SignedIn -= SignedIn;
                accounts.SignInFailed -= Failed;
                
                if (!success && accounts.IsSignedIn)
                {
                    accounts.SignOut();
                }
            }
        }

        public void SignOut()
        {
            if (PlayerAccountService.Instance.IsSignedIn)
            {
                PlayerAccountService.Instance.SignOut();
            }
        }
    }
}
