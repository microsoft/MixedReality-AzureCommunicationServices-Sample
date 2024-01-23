// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Azure.Communication.Calling.Unity.Rest
{
    internal static class Identity
    {
        /// <summary>
        /// Create a new "guest" identity for the Azure Communication Services.
        /// </summary>
        internal static Task<CreateResponse> Create(
            string endpoint,
            string accessKey,
            string[] scopes = null)
        {
            Client.ValidateEndpoint(endpoint);

            CreateRequest request = new CreateRequest()
            {
                createTokenWithScopes = scopes ?? new string[0]
            };

            return Client.Request<CreateRequest, CreateResponse>(
                HttpMethod.Post,
                $"{endpoint}/identities?api-version=2022-06-01",
                AuthenticationType.Key,
                accessKey,
                request);
        }

        /// <remarks>
        /// Call an Azure Function App which wraps the ACS's create identity API.
        /// This api can only be used in the the AAD access token.
        /// </remarks>
        internal static Task<CreateResponse> Create(
            string endpoint,
            TokenResponse functionAppAccess,
            string[] scopes = null)
        {
            Client.ValidateEndpoint(endpoint);

            IssueAccessTokenRequest request = new IssueAccessTokenRequest()
            {
                scopes = scopes ?? new string[0]
            };

            return Client.Request<IssueAccessTokenRequest, CreateResponse>(
                HttpMethod.Post,
                $"{endpoint}/api/CreateUserAndToken",
                AuthenticationType.Token,
                functionAppAccess.Token,
                request);
        }

        /// <summary>
        /// Create a new Teams identity for the Azure Communication Services from an AAD access token of a Teams User.
        /// </summary>
        internal static Task<ExchangeAccessTokenResponse> Exchange(
            string endpoint,
            string accessKey,
            TokenResponse communicationsAzureActiveDirectoryAccess)
        {
            Client.ValidateEndpoint(endpoint);

            ExchangeAccessTokenRequest request = new ExchangeAccessTokenRequest()
            {
                appId = communicationsAzureActiveDirectoryAccess.ClientId,
                token = communicationsAzureActiveDirectoryAccess.Token,
                userId = communicationsAzureActiveDirectoryAccess.UserId
            };

            return Client.Request<ExchangeAccessTokenRequest, ExchangeAccessTokenResponse>(
                HttpMethod.Post,
                $"{endpoint}/teamsUser/:exchangeAccessToken?api-version=2022-06-01",
                AuthenticationType.Key,
                accessKey,
                request);
        }

        /// <remarks>
        /// Call an Azure Function App which wraps the ACS's exchangeAccessToken API.
        /// This api can only be used in the the AAD access token.
        /// </remarks>
        internal static Task<ExchangeAccessTokenResponse> Exchange(
            string endpoint,
            TokenResponse functionAppAccess,
            TokenResponse communicationsAzureActiveDirectoryAccess)
        {
            Client.ValidateEndpoint(endpoint);

            ExchangeAccessTokenRequest request = new ExchangeAccessTokenRequest()
            {
                appId = communicationsAzureActiveDirectoryAccess.ClientId,
                token = communicationsAzureActiveDirectoryAccess.Token,
                userId = communicationsAzureActiveDirectoryAccess.UserId
            };

            return Client.Request<ExchangeAccessTokenRequest, ExchangeAccessTokenResponse>(
                HttpMethod.Post,
                $"{endpoint}/api/GetTokenForTeamsUser",
                AuthenticationType.Token,
                functionAppAccess.Token,
                request);
        }

        /// <summary>
        /// Issue a new access token for a "guest" identity. The token is for Azure Communication Services
        /// </summary>
        internal static Task<IssueAccessTokenResponse> Refresh(
            string endpoint,
            string accessKey,
            string id,
            string[] scopes)
        {
            Client.ValidateEndpoint(endpoint);
            Client.ValidateString(nameof(id), id);

            IssueAccessTokenRequest request = new IssueAccessTokenRequest()
            {
                scopes = scopes ?? new string[0]
            };

            return Client.Request<IssueAccessTokenRequest, IssueAccessTokenResponse>(
                HttpMethod.Post,
                $"{endpoint}/identities/{id}/:issueAccessToken?api-version=2022-06-01",
                AuthenticationType.Key,
                accessKey,
                request);
        }

        /// <remarks>
        /// Call an Azure Function App which wraps the ACS's issue access token API.
        /// This api can only be used in the the AAD access token.
        /// </remarks>
        internal static Task<IssueAccessTokenResponse> Refresh(
            string endpoint,
            TokenResponse defaultAzureActiveDirectoryAccessToken,
            string id,
            string[] scopes)
        {
            Client.ValidateEndpoint(endpoint);
            Client.ValidateString(nameof(id), id);

            IssueAccessTokenRequest request = new IssueAccessTokenRequest()
            {
                id = id,
                scopes = scopes ?? new string[0]
            };

            return Client.Request<IssueAccessTokenRequest, IssueAccessTokenResponse>(
                HttpMethod.Post,
                $"{endpoint}/api/GetToken",
                AuthenticationType.Token,
                defaultAzureActiveDirectoryAccessToken.Token,
                request);
        }

        [Serializable]
        public class CreateRequest : RestRequest
        {
            public string[] createTokenWithScopes { get; set; } = new string[0];
        }

        [Serializable]
        public class CreateResponse : RestResponse
        {
            public CreateResponseIndentity identity { get; set; } = new CreateResponseIndentity();
            public CreateResponseAccessToken accessToken { get; set; } = new CreateResponseAccessToken();
        }

        [Serializable]
        public class ExchangeAccessTokenRequest : RestRequest
        {
            public string appId { get; set; } = string.Empty;
            public string token { get; set; } = string.Empty;
            public string userId { get; set; } = string.Empty;
        }

        [Serializable]
        public class ExchangeAccessTokenResponse : RestResponse
        {
            public string token { get; set; } = string.Empty;
            public string expiresOn { get; set; } = string.Empty;
        }

        [Serializable]
        public class CreateResponseIndentity
        {
            public string id { get; set; } = string.Empty;
        }

        [Serializable]
        public class CreateResponseAccessToken
        {
            public string token { get; set; } = string.Empty;
            public string expiresOn { get; set; } = string.Empty;
        }

        [Serializable]
        public class IssueAccessTokenRequest : RestRequest
        {
            public string[] scopes { get; set; } = new string[0];

            /// <summary>
            /// The user's acs id. This is only used with the Function App wrapper
            /// </summary>
            public string id { get; set; } = string.Empty;
        }

        [Serializable]
        public class IssueAccessTokenResponse : RestResponse
        {
            public string token { get; set; } = string.Empty;
            public string expiresOn { get; set; } = string.Empty;
        }
    }
}
