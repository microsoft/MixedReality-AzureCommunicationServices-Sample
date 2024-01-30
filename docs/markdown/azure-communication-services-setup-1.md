# Setting Up Azure Communication Services

The sample app requirers setting up an [Azure Communication Services](https://docs.microsoft.com/azure/communication-services/overview) resource. This resource provides voice, video, chat, telephony, email, and Teams interoperability services. While there are many features, the sample mainly focuses on the Team interoperability. 

>This section provides a very quick overview of creating an Azure Communication Services (ACS) resource. For a more complete guide, visit [Quickstart: Create and manage Communication Services resources](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-azp).

To set up an ACS resource:

1. Go to the [Azure Portal](http://portal.azure.com) and sign-in with an account capable of creating an Azure resources.  
   
2. Click the **Create a resource** button.

3. Search for *"Communication Services"*.
   
4. On the **Create resource** page, click the **Create** button to start the creation process.
   
    <img src="./images/image-400-acs-setup.png" alt="Screenshot of Azure portal website and the 'create a resource' button" style="height:300px"/>
    <img src="./images/image-401-acs-setup.png" alt="Screenshot of Azure portal website creating a new Communication Services  resource" style="height:300px"/>

5. Select an Azure **Subscription** that will be billed when using the Communication Services.
   
6. Create or select an existing **Resource Group**. For more information on Resource Groups, visit [What is a resource group](https://learn.microsoft.com/azure/azure-resource-manager/management/manage-resource-groups-portal#what-is-a-resource-group).
   
7. Enter a **Resource Name** for the Communication Services resource.

8. Select the **Data location**. This cannot change once the resource is created. This location determines where the data will be stored at rest.

## Next Step
The next step, [Multi-tenant Accounts and Communication Services](./azure-function-setup-1.md#multi-tenant-accounts-and-communication-services),  introduces a process of setting up an existing Azure tenant so that multi-tenant accounts can access the newly created ACS resource.