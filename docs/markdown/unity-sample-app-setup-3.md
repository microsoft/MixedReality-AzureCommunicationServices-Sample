# Setup Microsoft Authentication Library (MSAL)
If the authentication type has been set MSAL, the setup is very simple and does not require an application specific URI.

To configured authentication with MSAL:

1. Sign in to the [Azure portal](https://portal.azure.com).
   
2. Go to the [Azure Activate Directory](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/Overview) blade.
   
3. Find and go to the [application registration](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/RegisteredApps).  
   
4. In the left-side navigation under Manage, select **Authentication**.
   
5. In the Configure platforms panel, click Mobile and desktop applications.

This step depends on whether your app hosts a browser in a UI control (an embedded browser) or uses the operating system's browser (a system browser). 

1. If your app uses an embedded browser: Under Configure Desktop + devices, select the first redirect URI:
   
    ```
    https://login.microsoftonline.com/common/oauth2/nativeclient.
    ```

2. If your app uses a system browser: In the Custom redirect URIs box, enter http://localhost. Make sure you enter http, and not https.
   
    > If you're a Unity user, this makes authentication work in Editor mode.
    
    <img src="./images/image-507-msal-auth-app-reg.png" alt="Adding MSAL support to a Windows apps by adding the default MSAL and localhost reply URIs to the Azure app's registration" style="max-height: 250px" />

## Next Optional Step
The next optional step, [Additional Admin Consent Information](./unity-sample-app-setup-4.md#additional-admin-consent-information), describes how to grant the sample app admin consent using the web browser.