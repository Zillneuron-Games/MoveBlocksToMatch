using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Zillneuron.Utilities;

public class SignInService : MonoSingleton<SignInService>
{
    public async Task<bool> Initialize()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in anonymously: " + AuthenticationService.Instance.PlayerId);
        }

        return true;
    }

    // Call this when user taps "Connect Account"
    public async void LinkWithPlatformAccount(string idToken)
    {
#if UNITY_ANDROID
        await SignInWithGoogle(idToken);
#elif UNITY_IOS
        await SignInWithApple(idToken);
#endif

        Debug.Log("Now linked to account! PlayerId: " + AuthenticationService.Instance.PlayerId);
    }

    private async Task SignInWithGoogle(string idToken)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGoogleAsync(idToken);
        }
        catch (AuthenticationException e)
        {
            Debug.LogError("Google sign-in failed: " + e.Message);
        }
    }

    private async Task SignInWithApple(string idToken)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithAppleAsync(idToken);
        }
        catch (AuthenticationException e)
        {
            Debug.LogError("Apple sign-in failed: " + e.Message);
        }
    }

    private void CheckLinkedAccounts()
    {
        var identities = AuthenticationService.Instance.PlayerInfo.Identities;

        foreach (var id in identities)
        {
            Debug.Log($"Provider: {id.TypeId}");
        }

        bool hasGoogle = identities.Exists(id => id.TypeId == "Google");
        bool hasApple = identities.Exists(id => id.TypeId == "Apple");

        if (hasGoogle)
        {
            Debug.Log("✅ Google is already linked.");
        }

        if (hasApple)
        {
            Debug.Log("✅ Apple is already linked.");
        }

        // You can now disable or hide buttons accordingly.
    }
}
