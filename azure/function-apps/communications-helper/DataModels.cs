// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Azure.Communication.Identity;

namespace azure_communications_helper_func
{
    [Serializable]
    internal class AccessTokenRequest
    {
        public CommunicationTokenScope[] scopes = new CommunicationTokenScope[0];
        public string id = string.Empty;
    }

    [Serializable]
    internal class ExchangeAccessTokenRequest
    {
        public string appId = string.Empty;
        public string token = string.Empty;
        public string userId = string.Empty;
    }


    [Serializable]
    public class IdentityAndAccessTokenResponse
    {
        public IdentityResponse identity = new IdentityResponse();
        public AccessTokenResponse accessToken  = new AccessTokenResponse();
    }

    [Serializable]
    public class IdentityResponse
    {
        public string id = string.Empty;
    }

    [Serializable]
    public class AccessTokenResponse
    {
        public string token = string.Empty;
        public DateTimeOffset expiresOn = DateTimeOffset.MinValue;
    }
}
