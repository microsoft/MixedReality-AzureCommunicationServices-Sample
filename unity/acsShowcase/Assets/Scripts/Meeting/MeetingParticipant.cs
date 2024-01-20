// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;

namespace Azure.Communication.Calling.Unity
{
    public class MeetingParticipant
    {
        internal MeetingParticipant(RemoteParticipant serviceValue)
        {
            ServiceValue = serviceValue;
            DisplayName = "Unknown";
            IsLocal = false;
            IsGuest = true;

            if (ServiceValue != null)
            {
                ServiceId = ServiceValue.Identifier;
                DisplayName = ServiceValue.DisplayName ?? DisplayName;
                IsGuest = DisplayName.EndsWith("(Guest)");
            }
        }

        internal MeetingParticipant(string displayName, bool isGuest)
        {
            DisplayName = displayName;
            IsLocal = true;
            IsGuest = isGuest;
        }

        internal static MeetingParticipant CreateRemote(RemoteParticipant serviceValue)
        {
            return new MeetingParticipant(serviceValue);
        }

        internal static MeetingParticipant CreateLocal(string displayName, bool isGuest)
        {
            return new MeetingParticipant(displayName, isGuest);
        }

        public bool IsLocal { get; }

        public bool IsGuest { get; }

        public string DisplayName { get; }

        public RemoteParticipant ServiceValue { get; }

        public CallIdentifier ServiceId { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}