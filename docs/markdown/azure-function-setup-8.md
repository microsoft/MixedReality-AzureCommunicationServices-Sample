# More Function App Configuration
The sample web app needs to know the Azure Communication Services (ACS) endpoint URI. The sample web app obtains the URI via a [Function App Configuration](https://docs.microsoft.com/azure/azure-functions/functions-how-to-use-azure-function-app-settings?tabs=portal#settings) setting. To set a configuration setting, go back to the Function App's resource page on the Azure Portal. 

To set the configuration setting:

1. Go to the [Azure Portal](https://portal.azure.com).
   
2. Find and go the [Azure Communication Services](https://portal.azure.com/#blade/HubsExtension/BrowseResourceBlade/resourceType/Microsoft.Communication%2FCommunicationServices) blade.
   
3. Copy the Azure Communication Services' endpoint URI for later.

4. Find and go to the [Function App](https://portal.azure.com/#view/HubsExtension/BrowseResource/resourceType/Microsoft.Web%2Fsites/kind/functionapp) blade.
   
5. Click the **Settings > Environment Variables** section.
   
6. Click **New application setting**. 
   
7. Name the new setting **COMMUNICATIONS_ENDPOINT**.
   
8. Set the setting's value to your ACS resource endpoint. For example, `https://{your-acs-resource-name}.communication.azure.com`. This value will be used when connecting to your ACS resource.

    ![Screenshot showing the function app's configuration settings within the Azure Portal](./images/image-10-function-app-configuration.png)

9.  After adding the new setting, be sure to click the **Apply** button to commit changes. The **Apply** button is at the bottom of the page.
   
10. While on the configuration tab, verify that there's a setting called **MICROSOFT_PROVIDER_AUTHENTICATION_SECRET**. This value is the same value as the web app registration's secret, called **Generated by App Service**. If this setting is missing, authentication will not work.

> The sample web application can be changed to not use this configuration setting. For example, while not recommended, the sample code could hardcode the Azure Communication Services endpoint URI. Nevertheless, this walk-through assumes that the sample will use the custom configuration setting named **COMMUNICATIONS_ENDPOINT**.

## Next Step
The next step, [Grant Function App Access to Communication Services](./azure-function-setup-9.md#grant-function-app-access-to-communication-services), describes how to grant the Function App resource access to your ACS resource.