// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.UnityClient;

namespace Azure.Communication.Calling.Unity
{
    public abstract class MeetingLocator
    {
        /// <summary>
        /// Create the internal representation of a meeting locator that is give the Azure Communications SDK when joining a meeting.
        /// </summary>
        public abstract JoinMeetingLocator CreateJoinMeetingLocator();
    }
}
