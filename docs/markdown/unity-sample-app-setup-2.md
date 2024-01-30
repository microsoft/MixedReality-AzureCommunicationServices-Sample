# Setup Web Authentication Manager (WAM)
When deploying the sample Unity application to the HoloLens 2, the app works with authentication based on WAM, which streamlines the log-in process for users on Windows devices. Note that in editor the app will use MSAL authentication instead.

> Using WAM requires registering an app package security identifier (SID) with the Azure native application registration. The package SID is part of the app specific reply URI that WAM uses during authentication.

Microsoft Authentication Library ([MSAL](https://docs.microsoft.com/azure/active-directory/develop/msal-overview)) Provides:
* Web-based UI that many users of Windows applications will be familiar with.
* The only authentication option available for use inside the Unity editor.
* Can be used with UWP applications for desktop or other devices.
* Requires no custom URI registration in the Azure portal.

Web Account Manager ([WAM](https://docs.microsoft.com/windows/uwp/security/web-account-manager)) Provides:
* Non-Web UI that can only be used with UWP applications.
* Provides a more OS-integrated authentication experience on HoloLens 2. It makes use of the user credentials already on the device so the user does not need to type a password in order to log in.
* Requires registration of an authentication ID as described below.

If the authentication type has been set WAM, the compiled app package won't be able to receive AAD tokens until the application's reply URI has been registered with the native app's Azure registration. That is, Windows provides each application package with a unique URI and ensures that messages sent to that URI are only sent to that application. This is the URI that needs to registered on your [Azure portal](https://portal.azure.com).

To determine the redirect URI for your app you will need the Package SID. To quickly and easily get the Package SID you can:

1. Download the [AppPackageInfo](https://github.com/najadojo/AppPackageInfo/releases) tool.
   
2. Build a Universal Windows Package (UWP) for the sample Unity project.

3. Get your app package path and run the tool on the command line. The tool will take any of the following:
   * package.appxmanifest
   * appxmanifest.xml
   * my package.appx
   * my package.appxbundle
   * my package.msix
   * my package.msixbundle

4. Move the AppPackageInfo.exe file into a folder with one of the files listed above.
   
5. Open the Context Menu: Hold shift and right click in the folder.
   
6. Select Open PowerShell Window Here.
   
7. In the PowerShell window, type the following, replacing *{your filename with extension}* with the your file: 

    ```PowerShell
    PS C:\> .\AppPackageInfo.exe .\{your filename with extension}
    ```
8. Copy the returned Package SID value to the clipboard.
9. 
   ![UWP App registration SID](./images/image-505-wam-auth-sid-sample.png)

    > There is a risk of getting a wrong value if you use just the appxmanifest.xml or package.appxmanifest where if the signing cert doesn't match the publisher, the package publisher will prefer the cert. If it isn't working, try using .appx, .appxbundle, .msix or .msixbundle.

10. Sign in to the [Azure portal](https://portal.azure.com).
   
11. Search for and select Azure Active Directory. In the left-side navigation under Manage, select App registrations.
   
12. Under Display Name, select your app.
    
13. In the left-side navigation under Manage, select **Authentication**.
    
14. Under Mobile and desktop applications, paste the following into the editable box located below the three listed options:
    
    ```
    ms-appx-web://Microsoft.AAD.BrokerPlugIn/<your Package SID>
    ```

15. Select Save.    

    <img src="./images/image-506-wam-auth-app-reg.png" alt="Adding WAM support by the app's reply URI to the Azure app's registration" style="max-height: 300px" />

    > WAM Authentication doesn't work within the Unity Editor. The app can only use WAM authentication when running inside a built app package, not the Unity Editor.

To validate that the WAM setup, the Unity project must be built and deployed to a HoloLen 2 device. However, there are a couple of optional steps to consider before building the Unity project. After that, build and deploy an app package to the HoloLens, and validate that the app can connect to ACS.

Before attempting to use WAM sign-in, first ensure the application has been given admin consent for the Teams' tenant. For more information on ways to grant admin consent, see the [Additional Admin Consent Information](unity-sample-app-setup-4.md#additional-admin-consent-information).

## Next Optional Step
The next optional, [Setup Microsoft Authentication Library (MSAL)](./unity-sample-app-setup-3.md#setup-microsoft-authentication-library-msal), describes how to configure the sample app with MSAL authentication. 