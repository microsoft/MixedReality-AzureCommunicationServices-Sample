// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Communication.Calling.Unity
{
    public interface ISignin
    {
        void Initialize(string clientId, string authority, string tenantId);

        Task<TokenResponse> GetTokenAsync(string[] scopes);

        Task<TokenResponse> GetTokenAsync(string[] scopes, string resource);

        Task<TokenResponse> GetTokenAsync(string[] scopes, CancellationToken cancellationToken);

        Task<TokenResponse> GetTokenAsync(string[] scopes, string resource, CancellationToken cancellationToken);

        Task ClearCacheAsync();

        String GetDeviceCode();
    }
}
