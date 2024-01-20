// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 


using Azure.Communication.Calling.UnityClient;

namespace Azure.Communication.Calling.Unity
{
    public abstract class UserIdentity
    {
        public abstract CallIdentifier CreateIdentifier();
    }
}
