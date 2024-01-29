# Multi-tenant Accounts and Communication Services

For multi-tenant accounts to access an Azure Communication Services (ACS) resource, as an authenticated Teams users, the application must first exchange an [Azure access token](https://docs.microsoft.com/azure/active-directory/develop/access-tokens) for an [ACS access token](https://docs.microsoft.com/azure/communication-services/quickstarts/access-tokens?tabs=windows&pivots=platform-azcli). The token exchange can only be securely done by a custom web service. Meaning that, to use ACS's identity APIs as an AAD user, the developer must first create and host their own web API. This custom service will exchange a multi-tenant AAD token for an ACS token.

This walk-through describes how to setup a custom web API, using Azure Functions, and demonstrates how to perform a multi-tenant token exchange using the ACS Identity SDK. For more information on authentication, visit [Single-tenant and multi-tenant authentication for Teams users](https://docs.microsoft.com/azure/communication-services/concepts/interop/custom-teams-endpoint-authentication-overview#case-2-example-of-a-multi-tenant-application).

![Sequence diagram detailing multi-tenant authentication](./images/authentication-case-multiple-tenants-hmac.png)

For more information on authentication, visit [Single-tenant and multi-tenant authentication for Teams users](https://docs.microsoft.com/azure/communication-services/concepts/interop/custom-teams-endpoint-authentication-overview#case-2-example-of-a-multi-tenant-application).

During development it might be easier to use an Azure authentication key, instead of setting up additional services. However, when using a key, app users will only be able to use Teams interoperability as "guests". To setup the sample with an authentication key, skip to [Configuring Sample with Authentication Key](./unity-sample-app-setup-7.md).

## Next Step
The next step, [Azure Function Setup](./azure-function-setup-2.md#azure-function-app-setup), describes how to create an Azure Function App.