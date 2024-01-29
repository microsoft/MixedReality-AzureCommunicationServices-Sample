# Azure Function App Setup
An Azure Function App is used by this sample to create Azure Communication Services (ACS) access tokens from Azure access tokens. To deploy this sample web application, first create a new Function App resource.

To create a the resource:

1. Go to the [Azure Portal](https://portal.azure.com) and sign-in with an account capable of setting up new Azure resources.

2. Click **Create a resource** and search for "Function App"

3. Select an Azure **Subscription** that will be charged when using this Function App
   
4. Create or select an existing **Resource Group**. For more information on Resource Groups, visit [What is a resource group](https://learn.microsoft.com/azure/azure-resource-manager/management/manage-resource-groups-portal#what-is-a-resource-group).

5. Enter a **Function App name**.
   
6. Set **Publish** to **Code**.

7. Set **Runtime stack** to **.Net**
   
8. Set the runtime **Version** to **6**
   
9.  Select a **Region** to host the Function App.
    
10. Set **Operating System** to **Windows**
    
11. Set **Plan type** to **Consumption (Serverless)**

    <img src="./images/image-01-create-function-app-resource.png" alt="Screenshot showing the creation of an Azure Function on the Azure portal website" style="height:400px"/>
    
12. Click **Next: Hosting**
   
13. The next setup is sets up an Azure Storage Account for the Function App.
    
14. Create a new Storage Account or select an existing Storage Account may be used. This walk-through assumes a new Storage Account is created.

15. Click through all the remaining setup steps, keeping default values.
    
16. Wait for your new Function App resource to be deployed. 

> For more on setting up Azure Functions and their pricing, visit [Introduction to Azure Functions](https://docs.microsoft.com/azure/azure-functions/functions-overview) and [Azure Functions Pricing](https://azure.microsoft.com/pricing/details/functions/).

## Next Step
The next step, [Sample Project Overview](./azure-function-setup-3.md#sample-web-app-overview), describes the sample web app that'll be deployed to the newly created Azure Function resource.