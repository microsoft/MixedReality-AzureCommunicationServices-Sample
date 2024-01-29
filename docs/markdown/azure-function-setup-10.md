# Native App Registration

In order for the Unity app to use the web app APIs, a native app registration has to be created. Instead of creating a new registration, this walk-through will reuse the registration created for the web app. However, if desired, a different app registration can be used. For simplicity, however, this walk-through will reuse the existing app registration.

To add a native app support to the existing web app registration:

1. Go to the [Azure Portal](https://portal.azure.com).
   
2. Go to the [Azure Active Directory](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade) blade.

3. Go to the [App registrations](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/RegisteredApps) section.

4. Search for the existing web app registration that was created in an earlier [step](azure-function-setup-5.md#adding-aad-authentication).

5. From the app registration page, click **Manage > Authentication**.
   
6. Click **Add a platform**.
   
7. Select **Mobile and desktop applications**. 
   
8. Add `http://localhost` redirect URI to the **Mobile and desktop application** section.

    ![Screenshot showing the AAD app registrations within the Azure Portal](./images/image-14-web-native-app.png)

    > Note, `http://localhost` is only used for development purposes. The sample code is currently configured to use `http://localhost` for authentication within the Unity Editor. When deploying to HoloLens, an additional redirect URI will be needed. These URIs are described in later [steps](unity-sample-app-setup-2.md#setup-web-authentication-manager-wam).

9. Click **Save** to save the configuration changes.

The Unity app requires an AAD access token to access Teams. To add the Teams permissions to the native app registration:

1. From the app registration page, click **Manage > API permissions**.
   
2. Click **Add a permission**.
   
3. Select **Azure Communication Services** under the **Microsoft APIs** header.
   
4. Select **Delegated permissions**, since the app will access Teams as a signed-in user.
   
5. Select the **Teams.ManageCalls** permission. The sample app requires this when joining or creating Teams calls or meetings when using a Teams identity.
   
6. Select the **Teams.ManageChat** permission. The sample app requires this to access Teams chat when using a Teams identity.
   
7. Click **Add** to confirm.

    ![Screenshot showing the app registration adding ACS permissions within the Azure Portal](./images/image-15-web-native-app.png)

The Unity app requires an AAD access token to access the users' calendar events, contacts, online meetings, and chat messages from the Microsoft Graph. To add Microsoft Graph permissions to the native app registration:

1. From the app registration page, click **Manage > API permissions**.
   
2. Click **Add a permission**.
   
3. Select **Microsoft Graph** under the **Microsoft APIs** header.

4. Select **Delegated permissions**, since the app will access the Microsoft Graph as a signed-in user.
   
5. Select the **Calendars.Read** permission. Required when requesting upcoming calendar events from [/me/events](https://learn.microsoft.com/en-us/graph/api/calendar-list-events?view=graph-rest-1.0&tabs=http). 
   
6. Select the **People.Read** permission. Required when requesting a list of relevant people from [/me/people](https://learn.microsoft.com/en-us/graph/api/user-list-people?view=graph-rest-1.0&tabs=http).

7. Select the **User.Read** permission. Required this for sign-in, and for reading basic company information.
   
8. Select the **Chat.ReadWrite** permission. Required when loading and sending chat messages.

9. Select the **OnlineMeetings.Read** permission. Required to determine the chat thread ID of an online meeting.
    
10. Select the **Presence.ReadWrite** permission. Required to query other users' online presences and to set the current user's online presence.
   
If using a separate app registration for the web app, the Unity app will require an AAD access token to access the sample web app. To add custom web app permissions to the native app registration:

1. Go to the app registration page.
   
2. Click **Manage > API permissions**.
   
3. Click **Add a permission**.
   
4. Click the **My APIs** heading.
   
5. Search for the new web API that was exposed in [Complete Web App Registration Setup](./azure-function-setup-6.md#complete-web-app-registration-setup). 
    
6. Select **Delegated permissions**, since the app will access the Microsoft Graph as a signed-in user.
   
7. Select the **user_impersonation** permission. The sample app requires this when accessing the sample web app API. This is not required if the web app and native app share the same app registration.
   
8. Click **Add permissions**.

    ![Screenshot showing the app registration adding the 'My API' permission within the Azure Portal](./images/image-17-web-native-app.png)

The native app registration should now have the following API permissions listed:

* Azure Communication Services' *Teams.ManageCalls*
* Azure Communication Services' *Teams.ManageChats*
* Microsoft Graph's *Calendar.Read*
* Microsoft Graph's *People.Read*
* Microsoft Graph's *User.Read*
* Microsoft Graph's *Chat.ReadWrite*
* Microsoft Graph's *OnlineMeetings.Read*
* Microsoft Graph's *Presence.ReadWrite*
* Custom Web App's *user_impersonation*, if web and native app share different app registrations.

 ![Screenshot showing the app registration's API permissions within the Azure Portal](./images/image-16-web-native-app-permissions.png)

Your Azure setup is now complete. The sample Unity app can now be updated to connect to your newly configured resources.

> Optionally, if the web app and native app have unique app registrations, the native client app may need granted access to the web API. From the web app registration page go to **Manage > Expose an API** and click **Add a client application**, under the **Authorized client applications**. Add the native app registration's application (client) ID, and select the custom API scope created earlier.

## Next Step
 The next step, [Configuring Sample with AAD Authentication](./unity-sample-app-setup-1.md), describes how to configure the Unity sample app
 with AAD authentication.