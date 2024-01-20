// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Communication.Calling.Unity
{
    /// <summary>
    /// The result from a ISignin.GetTokenAsync() request.
    /// </summary>
    public struct TokenResponse
    {
        /// <summary>
        /// The access token string.
        /// </summary>
        public string Token;

        /// <summary>
        /// The user's object ID received after authentication.
        /// </summary>
        public string UserId;

        /// <summary>
        /// The user's tenant ID used when requesting the access token
        /// </summary>
        public string TenantId;

        /// <summary>
        /// The application (client) ID used when requesting the access token
        /// </summary>
        public string ClientId;

        /// <summary>
        /// Get an invalid token response
        /// </summary>
        public static TokenResponse Invalid { get; } = new TokenResponse();
    }

    public static class TokenResponseExtensions
    {
        public static bool IsValid(this TokenResponse tokenResponse)
        {
            return !string.IsNullOrWhiteSpace(tokenResponse.Token);
        }
    }
}