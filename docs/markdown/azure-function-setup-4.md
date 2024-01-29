# Deploying Sample Web App

Time to deploy the sample web app to an Azure Function App resource. [Visual Studio Code](https://code.visualstudio.com/) can be used to accomplish this. Install and launch Visual Studio Code, then open the directory containing the sample web app project. This directory can be found within this [repository](https://github.com/microsoft/MixedReality-AzureCommunicationServices-Sample) under the [azure/function-apps/communications-helper](https://github.com/microsoft/MixedReality-AzureCommunicationServices-Sample/tree/main/azure/function-apps/communications-helper) directory.

To deploy the web app:

1. Open the [azure/function-apps/communication-helper](https://github.com/microsoft/MixedReality-AzureCommunicationServices-Sample/tree/main/azure/function-apps/communications-helper) directory in [Visual Studio Code](https://code.visualstudio.com/).
   
    ![Screenshot showing the sample Function App code in Visual Studio Code](./images/image-201-function-app-code.png)

2. Install the [Azure Tools](https://marketplace.visualstudio.com/items?itemName=ms-vscode.vscode-node-azure-pack) extension by searching for it under Visual Studio Code's extension tab.

    ![Screenshot showing the Azure Tools extension in Visual Studio Code](./images/image-02-deploy-azure-fucntion.png)

3. Follow the Azure Tools extension setup instructions.
   
4. Sign into your Azure Subscription.
   
5. From the Azure extension Tab, locate your newly created Azure Function App resource.
   
6. Right-click your Function App resource and select **Deploy to Function App...**.
   
    ![Screenshot showing the Azure Tools extension in Visual Studio Code. The extension is now deploying the sample code to the Function App](./images/image-03-deploy-azure-fucntion.png)

7. Deployment will take a minute or two.

## Next Step
The next step, [Adding AAD Authentication](./azure-function-setup-5.md#adding-aad-authentication), describes how to secure the newly create Function App resource with Azure Active Directory (AAD) authentication.