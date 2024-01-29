# Configuring Sample with Authentication Key
During development it might be easier to avoid setting up AAD authentication, and instead use an Azure authentication key to authenticate with the Azure Communication Services (ACS). If using an authentication key, be careful not to commit the key into source control, and avoid shipping production applications using the key directly. Authentication keys should remain secret and be kept secure.

First find the key for the ACS resource:

1. Sign in to the [Azure portal](https://portal.azure.com).
   
2. Search for and select the Communication Services resource.
   
3. Select **Settings > Keys**
   
4. Copy one of the two keys
      
Next paste this the key into the Unity project:

1. Open the Unity app project.
   
2. Open the **SampleScene** scene.
   
3. Find and select the **MeetingManager** object in the scene's hierarchy.
   
4. Paste the authentication key into the inspector window.
   
5. Clear the **Authentication Manager**, **Communication Authentication Token**, **Function App Endpoint**, and **Function App  Authentication Token** settings. These aren't needed if a key is being used. Having these fields set to some value can prevent the authentication key from being used.
   
    <img src="./images/image-508-auth-key-app.png" alt="Entering the auth key into MeetingManager's inspector properties" style="max-height: 350px" />

6. The application is now configured to use the Azure Communication Services with an authentication key. 

    > For development purposes, you can also use a communication user access token for authentication. This can be generated from **Settings > Identities & User Access Tokens** from the ACS resource.

# The End
 This is the of the sample app documentation. To learn about the Unity sample application, and how setup with
 AAD, go to the [start](./azure-function-setup-1.md#multi-tenant-accounts-and-communication-services) of this documentation
