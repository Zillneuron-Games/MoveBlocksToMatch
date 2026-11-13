# Platform sign-in setup

Blockmates uses Google Play Games on Android and Sign in with Apple on iOS. Each platform has its own Unity Authentication player and Cloud Save records. Guest profiles stay on the device.

## Android

1. In Play Console, configure Play Games Services for the Android app. Register the Android OAuth credential for the app's package name and signing certificate, and create a Web OAuth credential for server-side access.
2. In Unity, open **Window > Google Play Games > Setup > Android Setup** and enter the Play Games resources and Web client ID. This generates the settings asset and Android manifest used by the plugin.
3. Enable **Google Play Games** in Unity Authentication for the game project and configure the Web OAuth client ID and secret there. Keep the secret out of the project and player build.
4. Resolve Android dependencies with the existing dependency resolver. Test with a signed Android build and an authorized Play Games tester.

## iOS

1. Enable **Sign in with Apple** on the app's identifier in Apple Developer and regenerate the provisioning profile if needed.
2. Enable **Sign in with Apple** in Unity Authentication using the same iOS Bundle ID. The build postprocessor adds the capability to the generated Xcode project.
3. Build and sign on macOS, then test on a physical iOS device.

## Smoke test

On each device, try guest play, cancel a sign-in, register with a platform identity that is new to Blockmates, sign out, then sign into that account again after making better progress as a guest. Check that registration carries the guest's hints and settings, returning accounts keep their own hints and settings, and sign-out keeps the account save available for the next login. Test a failed network request while signing in and confirm the active guest save stays available.
