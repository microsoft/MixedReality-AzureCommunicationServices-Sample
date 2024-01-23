// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;

namespace Azure.Communication.Calling.Unity
{
    /// <summary>
    /// The user access token and id for an existing "bring your own identity" user. These are users
    /// created via the Azure Cummunication Services identity APIs.
    /// </summary>
    [Serializable]
    public struct BringYourOwnIdentityToken
    {
        public string id;

        public string token;
    }
}
