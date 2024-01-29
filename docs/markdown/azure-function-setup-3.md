# Sample Web App Overview
Once you've created a Function App resource, it's time to deploy the sample web app. The sample project can be located within this repository under the [azure/function-apps/communications-helper](https://microsoft.visualstudio.com/Analog/_git/oss.mixedreality.acs.sample?path=/azure/function-apps/communications-helper) directory.

![Screenshot showing the sample Function App code in Visual Studio Code](./images/image-200-function-app-code.png)

 The sample project implements three REST APIs. These APIs are utilized by the Unity sample:

* **CreateUserAndToken**:  Creates new Azure Communication Services (ACS) Identity, and returns an ACS access token for the new identity. This identity is called a Bring Your Own Identity (BYOI). These identities can be used to connect to a Teams meeting, but the user will be joining as a guest. The returned access token will expire after a certain amount of time.
  
* **GetToken**: Used to create an ACS access token for an existing BYOI. The token returned here will also expire after a certain amount of time.
  
* **GetTokenForTeamsUser**: Used to create an ACS access token for a Teams Work/School identity. When calling this API, the application must send along a valid Azure access token with the appropriate ACS scopes (Teams.ManageCalls and/or Teams.ManageChat). The returned ACS access token will expire after a certain amount of time. To refresh a Teams user token, call **GetTokenForTeamsUser** again.

The request and response schemas for these functions match the schemas for the ACS REST APIs described in the [ACS Communication Identity](https://docs.microsoft.com/en-us/rest/api/communication/communication-identity) documentation.

## Next Step
The next step, [Deploying Sample Web App](./azure-function-setup-4.md#deploying-sample-web-app), describes how to deploy this sample project to the Azure Function resource.




