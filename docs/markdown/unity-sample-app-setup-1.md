# Configuring Sample with AAD Authentication
The Azure Communication Services (ACS) Unity sample can now be configured to use AAD authentication. To use this type of authentication, the app requires a custom REST/App service. If you haven't already, please follow the setup steps starting at [Creating a Communication Services Resource](./azure-communication-services-setup-1.md#setting-up-azure-communication-services).

To complete this configuration, following pieces of information are needed:

* **Azure Communication Services Endpoint**. URI can be found at the [Azure Communication Services](https://portal.azure.com/#blade/HubsExtension/BrowseResourceBlade/resourceType/Microsoft.Communication%2FCommunicationServices) blade on the [Azure Portal](https://portal.azure.com).
  
* **Azure Function App Endpoint**. URI can be found at the [Function App](https://portal.azure.com/#view/HubsExtension/BrowseResource/resourceType/Microsoft.Web%2Fsites/kind/functionapp) blade on the [Azure Portal](https://portal.azure.com).
  
* **Application (client) ID**. GUID is found on the native [application registration](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/RegisteredApps) overview section.
  
* **Directory (tenant) ID**. GUID is found on the native [application registration](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/RegisteredApps) overview section.
  
* **Custom Web API Scope URI**. URI can be found on the web [application registration](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/RegisteredApps), under the **Expose an API** section. This URI should look similar to `api://{web-app-tenant-id}/{web-app-client-id}/user_impersonation`.

To configure the Unity app with AAD authentication:

1. Open the sample Unity app project.
   
2. Open the **SampleScene** scene file. 
   
3. If prompted, install the TextMesh Pro components by clicking the **Import TMP Essentials** button. Without this component some text assets will not render correctly.

4. From the scene's hierarchy select the **MeetingManager** object.
   
5. In the inspector window set the **Communication Endpoint** to the endpoint URI for the ACS resource.
   
6. Also in the inspector window set the **Function App Endpoint** to the endpoint URI for the sample web app API.

   <img src="./images/image-501-unity-setup-meeting-mgr.png" alt="Screenshot showing the user editing the meeting manager properties within the Unity Editor" style="max-height: 300px" />

7. Next select the **AuthenticationManager** object in the hierarchy window. 
   
8. Go to inspector window and set **Client ID** to the application (client) id for your native application registration. 

   <img src="./images/image-502-unity-setup-auth-mgr.png" alt="Screenshot showing the user editing the authentication manager properties within the Unity Editor" style="max-height: 350px" />

9. Next select the **FunctionAppAccess** object in the hierarchy window. 
    
10. Go to inspector window and add a **Scope** to your custom web API scope URI. This URI should look similar to `api://{web-app-tenant-id}/{web-app-client-id}/user_impersonation`.

    <img src="./images/image-503-unity-setup-func-token.png" alt="Screenshot showing the user editing the azure function app token properties within the Unity Editor" style="max-height: 350px" />

You can now validate your configuration within the Unity Editor by:

1. Click the editor's **play** button.
   
2. Click the app menu's **Sign In** button. 

    <img src="./images/image-504-unity-validate-log-in-1.png" alt="Screenshot showing the menu in this sample app, while playing in the Unity Editor" style="max-height: 300px" />

3. If the app was setup successfully, a browser window should launch, asking for the user's AAD credentials. Enter the user's AAD credentials.
   
4. If this is the first time signing into the application, a admin dialog may appear. This dialog will request admin consent before continuing. If no such dialog appears, skip to step 8.
   
5. If admin consent is requested, select **sign-in as administrator** and enter administrator credentials  for the user's tenant. Note, this tenant is not necessarily the same tenant hosting the Communication Services. 
    
   > To connect to ACS as an authenticated Teams users, administrator authorization may be needed to use the sample application. This means, the first time a Teams user authenticates with ACS, using this application, the Teams user may need to be an administrator for their tenant. 
   
   > Another way to grant administrator consent is by entering this URL into a browser. For information on how to do this, read the [Additional Admin Consent Information](./unity-sample-app-setup-4.md#additional-admin-consent-information) section.
   
   
6. If admin consent is request, the administrator must now grant the application permissions for the entire organization.
   
7. Attempt to sign into the application again. The application should now receive an Azure Active Directory access token. The application then calls the deployed Function App to exchange the Azure Active Directory access token for a Communication Services identity access token.
   
8. If everything worked correctly, the app should display a list of relevant contacts and any upcoming meetings. The first time a user joins a meeting, a permissions dialog may appear. Grant the application the requested user permissions. 

## Next Step
Finally, the last step is to configure the app's authentication type. The first option is WAM authentication, as discussed in the next section, [Setup Web Authentication Manager (WAM)](unity-sample-app-setup-2.md#setup-web-authentication-manager-wam).