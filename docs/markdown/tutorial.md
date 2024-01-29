# A Simple Windows Tutorial App

## Introduction
A more simplified Azure Communication Services Calling application can be found under the [tutorial\Azure.Communication.Calling.Tutorial](./../../tutorial/Azure.Communication.Calling.Tutorial) directory. This is an UWP XAML application with a bare bones implementation. 

This sample uses a hardcoded **Teams Join URL** and **User Access Token** to connect to an existing Teams meeting. However, the bare bones implementation does provide a more straightforward example for how to use the ACS Calling SDK.

## Prerequisites
1.	Install [Visual 2019/2022](https://visualstudio.microsoft.com/downloads/)
2.  Install [Windows 10.0+ SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/)
4.  Install [git lfs](https://git-lfs.github.com/)
5.	Clone [repository](https://microsoft.visualstudio.com/Analog/_git/oss.mixedreality.acs.sample)
6.  Setup [Azure Communication Services](.\azure-communication-services-setup-1.md#setting-up-azure-communication-services) (only the ACS resource is needed for this tutorial app)
   
## Building Tutorial Sample
Open the [Azure.Communication.Calling.Tutorial](./../../tutorial/Azure.Communication.Calling.Tutorial/Azure.Communication.Calling.Tutorial.sln) solution file, build the solution, and deploy the application to the local PC.

## Running Tutorial Sample
Enter in a Teams meeting join URL and an ACS user access token. An ACS user access token can be created from the [Azure Portal](https://portal.azure.com).

To use the tutorial sample app:

1. Go to the [Azure Portal](https://portal.azure.com).
   
2. Go to the [Azure Communication Services](https://portal.azure.com/#blade/HubsExtension/BrowseResourceBlade/resourceType/Microsoft.Communication%2FCommunicationServices) blade for newly create ACS resource.
   
3. Select **Identities & User Access Tokens**
   
4. Check the **Voice and video calling (VOIP)** and **Chat** boxes.   

    <img src="./images/image-513-create-user-access-token.png" alt="A screenshot showing how to generate an ACS user access token on the Azure Portal."  style="max-height: 200px" />

6. Click **Generate** button.
   
7. Copy the resulting **User Access token**.

8. Launch the tutorial application
   
9.  Paste **User Access token** into the tutorial application
    
10. Paste some **Teams Join URL** into the tutorial application.

    <img src="./images/image-514-tutorial-app.png" alt="A screenshot showing how to use the tutorial application."  style="max-height: 350px" />

11. Click **Start** in the tutorial application.

12. The tutorial will then connect to the existing Teams meeting. 

13. Using another Teams client, PC, mobile, or web, connect to the same Teams meeting, and verify that the sample connected correctly.

## The End
This is the of the sample app documentation. To learn about the Unity sample application, and how setup with AAD, go to the [start](overview.md#azure-communication-services-for-hololens-2) of this documentation.


