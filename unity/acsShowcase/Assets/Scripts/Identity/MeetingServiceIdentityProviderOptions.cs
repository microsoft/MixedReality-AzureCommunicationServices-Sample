// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using System.Threading.Tasks;
using System;

namespace Azure.Communication.Calling.Unity
{
    public struct MeetingServiceIdentityProviderOptions
    {
        /// <summary>
        /// The endpoint for the Azure Communication Services
        /// </summary>
        public string communicationEndpoint;

        /// <summary>
        /// The user access token and id for an existing "bring your own identity" user. These are users
        /// created via the Azure Cummunication Services identity APIs. If provided, this will be used 
        /// instead of the obtaining an ACS access token from the configured function app, and instead of the 
        /// authentication key.
        /// </summary>
        public BringYourOwnIdentityToken communicationUserAccessToken;

        /// <summary>
        /// The Azure Key for the Azure Communications Services. If provided, this will be used 
        /// instead of the obtaining an ACS access token from the configured function app.
        /// </summary>
        public string communicationKey;

        /// <summary> 
        /// The Azure Active Directory access token containing Azure Communication Services scopes. This will be exchanged for
        /// an Azure Communication Services Identity access token, via the Funciton App wrappers.
        /// </summary>
        public TokenResponse communicationAzureActiveDirectoryAccess;

        /// <summary>
        /// The endpoint for the Azure Function App which wraps the Azure Communication Services access token creation.
        /// </summary>
        public string functionAppEndpoint;

        /// <summary>
        /// The access token with Azure Function App scopes.
        /// </summary>
        public TokenResponse functionAppAccess;

        /// <summary>
        /// The access token with Microsoft Graph scopes
        /// </summary>
        public TokenResponse graphAccessToken;

        /// <summary>
        /// A display name used if joining as a guest.
        /// </summary>
        public string displayName;
    }
}
