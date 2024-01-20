// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

namespace Azure.Communication.Calling.Unity
{
    public interface IAuthenticationRequest
    {
        /// <inheritdoc/>
        event AuthenticationEventDelegate OnAuthenticationEvent;

        /// <inheritdoc/>
        TokenResponse TokenResponse { get; }
    }
}