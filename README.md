# Azure Communication Services for HoloLens 2

## Introduction 
This sample demonstrates how to use the Azure Communication Services (ACS) within an immersive mixed reality application, running on HoloLens 2.

## Getting Started

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)
2. Install [Windows 10.0+ SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/)
3. Install [Unity 2022 LTS](https://unity3d.com/get-unity/download) with Universal Windows Platform Build Support
4. Install [.Net 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
5. Install [git lfs](https://git-lfs.github.com/)
6. Clone [repository](https://github.com/microsoft/MixedReality-AzureCommunicationServices-Sample)
7. For AAD authentication, obtain access to a work/school Teams account with administrative privileges.

## Overview

[Azure Communication Services](https://docs.microsoft.com/azure/communication-services/overview) (ACS) are cloud-based services backed by REST APIs and client library SDKs that help developers integrate voice, video, chat, telephony, and email communications into their applications.  While ACS provides many features, this sample mainly focuses on the Teams interoperability, such as joining a Teams meeting.

![A screenshot of the application's main panel and video call experience. The left  shows a panel with the signed-in user's profile picture and name, along with an upcoming meeting and a list of relevant contacts. The right shows a video panel with call controls.](./docs/markdown/images/acs-main-panel-in-call.png)

The sample app utilizes the following ACS features:

* Connecting to ACS as a "bring your own identity" (BYOI) user.
* Connecting to ACS as an authenticated Teams users, via a work/school AAD.
* Joining a Teams meeting as a Teams or guest (BYOI) user.
* Participating in voice and video communications during a Teams meeting.

> To sign into ACS as Teams user, an administrator must grant the sample app access to the Teams' tenant. This means, the first time a Teams user authenticates with ACS, using this application, the Teams user must be an administrator for their tenant.

There are many more ACS features not covered by this sample application. Please visit the main ACS [documentation](https://docs.microsoft.com/azure/communication-services/overview) for additional features and pricing.

## App Components

This immersive HoloLens 2 sample is made up of two parts:

* **Unity App**. A native application that runs on HoloLens 2 devices.
* **Web App**. An Azure Function application that helps create ACS access tokens from AAD access tokens.

<!-- This "break-page" diff is only used when merging MDs into a single file. --->
<div class='break-page'></div>

The following Microsoft components are used by this sample:

* **[ACS Calling SDK](https://docs.microsoft.com/azure/communication-services/concepts/voice-video-calling/calling-sdk-features)**. Unity app uses this to connect to ACS meetings and participate in video calls.
* **[ACS Identity SDK](https://docs.microsoft.com/azure/communication-services/concepts/identity-model)**. Used by the sample Function App to obtain ACS access tokens.  
* **[Azure Identity SDK](https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme)**. Also used by the sample Function App to obtain ACS access tokens.
* **[Azure Functions SDK](https://docs.microsoft.com/azure/azure-functions/functions-develop-vs?tabs=in-process)**. Used by the Azure Function App.
* **[Mixed Reality Toolkit 3](https://github.com/Microsoft/MixedRealityToolkit-Unity)**. Used to create mixed reality user experiences in the Unity app.
* **[Web Account Manager (WAM) for UWP](https://docs.microsoft.com/windows/uwp/security/web-account-manager)**. One of the authenticated methods used by the Unity app. 
* **[Microsoft Authentication Library (MSAL)](https://docs.microsoft.com/azure/active-directory/develop/msal-overview)**. Another one of the authenticated methods used by the Unity app when playing in the Unity editor

![Block diagram detailing the components used in this sample application](./docs/markdown/images/acs-on-hololens-2-block-diag.png)

## Next Step
The next step, [Setting Up Azure Communication Services](./docs/markdown/azure-communication-services-setup-1.md#setting-up-azure-communication-services), will walk you through how to create an Azure Communication Services resource.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.