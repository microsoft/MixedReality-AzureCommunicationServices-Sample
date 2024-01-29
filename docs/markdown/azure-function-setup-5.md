# Adding AAD Authentication

It's time to secure the Azure Function App resource with an Azure Active Directory (AAD) authentication provider. This walk-through assumes the Function App will use an AAD authentication provider, however the Function App can be configured to use other types of providers. If more information is required about Function App setup and its configuration, please visit [Authentication and authorization in Azure App Service and Azure Functions](https://docs.microsoft.com/en-us/azure/app-service/overview-authentication-authorization#authentication-flow).

To add an AAD authentication provider:

1. Go to the [Azure Portal](https://portal.azure.com).

2. Find and go to the [Function App](https://portal.azure.com/#view/HubsExtension/BrowseResource/resourceType/Microsoft.Web%2Fsites/kind/functionapp) blade.
   
3. Go to the Azure Function App resources's **Settings > Authentication** tab.
      
4. Click **Add identity provider**.

5. Select the **Microsoft** identity provider.
   
6. Click **Create new app registration** for the app registration type. An existing app registration can be used, but for simplicity this walk-through assumes you'll be creating a new app registration.
   
   <img src="./images/image-09-add-auth.png" alt="Screenshot showing the creation of a authentication provider for a Function App" style="max-height:500px"/>

7. Select the appropriate account type of your application. However, for this walk-through select **Any Azure AD directory - Multi-tenant**.  
   
8. Select **Require authentication** for the restrict access settings. 
   
9.  Since this is a web API, it's recommended to select the **HTTP 401** error response for the unauthenticated requests settings.

10. Click **Next**
   
11. Select the Microsoft Graph permissions the web API will request. 
   
12. Keep the default **User.Read** Microsoft Graph permission.
   
13. If needed, you can add additional permissions via the **Add permissions**. More permissions will and can be added later.
    
14. Click the **Add** button to complete the identity provider setup. 
    
    <img src="./images/image-10-add-auth.png" alt="Screenshot showing the app permissions of the new app registration" style="max-height:400px"/>

## Next Step
The next step, [Complete Web App Registration Setup](./azure-function-setup-6.md#complete-web-app-registration-setup), describes how to configure the new app registration so that it can properly expose a web API that is accessible to multi-tenant accounts.