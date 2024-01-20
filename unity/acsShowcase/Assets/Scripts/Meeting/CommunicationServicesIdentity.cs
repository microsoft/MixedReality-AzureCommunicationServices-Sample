// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.UnityClient;
using Azure.Communication.Calling.Unity.Rest;

namespace Azure.Communication.Calling.Unity
{
    public sealed class CommunicationServicesIdentity : UserIdentity
    {
        IUser _person;

        public CommunicationServicesIdentity(IUser person)
        {
            _person = person;
        }

        public override CallIdentifier CreateIdentifier()
        {
            return new UserCallIdentifier(_person.id);
        }

        public override string ToString()
        {
            return $"CommunicationServicesIdentity({_person?.displayName}@{_person?.id})";
        }
    }
}
