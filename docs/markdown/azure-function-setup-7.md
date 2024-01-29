# Update Function App's Token Audience
Since the resource (or Application ID) URI was changed in the previous [step](./azure-function-setup-6.md#complete-web-app-registration-setup), the Azure Function App's identity provider audience (i.e. the resource URI) needs to be updated. The default resource URI wasn't changed, you may skip to the [next step](./azure-function-setup-8.md#more-function-app-configuration).

To update the Function App resource's identity provider audience:

1. Go to the [Azure Portal](https://portal.azure.com).

2. Go to the [Function App](https://portal.azure.com/#view/HubsExtension/BrowseResource/resourceType/Microsoft.Web%2Fsites/kind/functionapp) blade.
   
2. Click the **Settings > Authentication** section
   
3. Click the **Edit** button for the Microsoft identity provider that was added in an earlier [step](azure-function-setup-5.md#adding-aad-authentication).
   
4. Under  **Allow token audiences**, change the resource URI to the **Application ID URI** created in the previous step. For example, change  `api://{apps-client-id}` to `api://{apps-tenant-id}/{apps-client-id}`. Making that the **Allowed token audience URI** matches **Application ID URI** from the [previous step](./azure-function-setup-6.md#complete-web-app-registration-setup).
   
5. Click **Save** to commit the changes.
   
    <img src="./images/image-101-web-app-function-app.png" alt="Screenshot showing the function app's token audience setting within the Azure Portal" style="max-height: 450px" />

## Next Step
The next, [Complete Web App Registration Setup](./azure-function-setup-8.md#more-function-app-configuration), describes some additional configuration changes that are specific to this sample app.