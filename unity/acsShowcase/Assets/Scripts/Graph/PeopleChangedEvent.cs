// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Azure.Communication.Calling.Unity
{

    [Serializable]
    public class PeopleChangedEvent : UnityEvent<PeopleChangedEventArgs>
    { }

    [Serializable]
    public class PeopleChangedEventArgs
    {
        public PeopleChangedEventArgs(IReadOnlyList<IUser> people)
        {
            People = people;
        }

        public IReadOnlyCollection<IUser> People { get; private set; }
    }
}
