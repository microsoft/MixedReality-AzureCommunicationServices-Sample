# Grant Function App Access to Communication Services

By default the new Azure Function App resource will not have access to the Azure Communication Services (ACS) resource. These steps will guide you through how to grant the Function App access.

To grant access:

1. Go to the [Azure Portal](https://portal.azure.com).

2. Find and go to the [Function App](https://portal.azure.com/#view/HubsExtension/BrowseResource/resourceType/Microsoft.Web%2Fsites/kind/functionapp) blade.

3. Select the **Settings > Identity** section.
   
4. Turn on the **System assigned** managed identity.
   
5. Click **Save** and confirm the changes.

   <img src="./images/image-17-function-app-id.png" alt="Screenshot showing the function app's identity settings within the Azure Portal" style="max-height: 200px"/>

6. Next, go to your [Azure Communication Services](https://portal.azure.com/#blade/HubsExtension/BrowseResourceBlade/resourceType/Microsoft.Communication%2FCommunicationServices) blade.
   
7. Click the **Access control (IAM)** section.
   
8. Click the **Add** button to add a new role assignment. If the **Add** button is disabled, the signed in account likely doesn't have the necessary privileges to add a role assignments.
   
9. Select the **Contributor** role, and click **Next**.

   ![Screenshot showing the Azure Communication Services resource's access control settings within the Azure Portal](./images/image-19-acs-role.png)

10. Assign access to a **Manage identity**.
    
11. Click **Select members**.
    
12. In the pop-up search for and select the newly create Function App resource.
    
13. Click **Next** and **Review + Assign** to complete the role assignment.

    ![Screenshot showing the Azure Communication Services resource's access control settings within the Azure Portal. The panel shows given access to the newly created Function App](./images/image-20-acs-role.png)

14. The Function App resource can now access your Communication Services resource. 

## Next Step
The next step, [Native App Registration](./azure-function-setup-10.md#native-app-registration), describes how to setup a native app registration so that sample's Unity app can access the sample's web app APIs.