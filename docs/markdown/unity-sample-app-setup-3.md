# Before Building Unity App
Before building the Unity app, the following may be configured:

* **Teams Meeting Join URL**. It is possible to hardcode a meeting join URL for testing.  The application will display upcoming meetings for the signed in user, and the app can join any meeting in this list. So a hardcode join URL is not required if the in app meeting list is sufficient.
  
* **Authentication Type**. Microsoft Authentication Library ([MSAL](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-overview)), or Windows Web Account Manager ([WAM](https://docs.microsoft.com/en-us/windows/uwp/security/web-account-manager)), or authentication key.

Setting the Teams Meeting URL is easy:

1. First, if needed, copy a Teams meeting URL from a upcoming meeting. 

2. Open the Unity project's **Main** scene.
  
3. Locate the **EventsManager** object in the scene's hierarchy.
   
4. Edit the **Static Events** array
   
5. Paste the Teams meeting URL into the inspector window.
    
    <img src="./images/image-510-default-teams-url.png" alt="Screenshot Teams meeting URL being entered into the Unity inspector window" style="max-height: 250px" />

6. Now, after authentication, the HoloLens 2 will display this hardcoded meeting and the user's upcoming meetings.

## Next Step
Finally, the last step is to configure the app's authentication type. The first option is WAM authentication, as discussed in the next section, [Setup Web Authentication Manager (WAM)](unity-sample-app-setup-4.md#setup-web-authentication-manager-wam).