// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Communication.Calling.UnityClient;
using System;

namespace Azure.Communication.Calling.Unity
{
    public static class MeetingCallStateExtensions
    {
        public static MeetingCallState ToUnity(this CallState value)
        {
            switch (value)
            {
                case CallState.None:
                    return MeetingCallState.None;

                case CallState.EarlyMedia:
                    return MeetingCallState.EarlyMedia;

                case CallState.Connecting:
                    return MeetingCallState.Connecting;

                case CallState.Ringing:
                    return MeetingCallState.Ringing;

                case CallState.Connected:
                    return MeetingCallState.Connected;

                case CallState.LocalHold:
                    return MeetingCallState.LocalHold;

                case CallState.Disconnecting:
                    return MeetingCallState.Disconnecting;

                case CallState.Disconnected:
                    return MeetingCallState.Disconnected;

                case CallState.InLobby:
                    return MeetingCallState.InLobby;

                case CallState.RemoteHold:
                    return MeetingCallState.RemoteHold;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static CallState FromUnity(this MeetingCallState value)
        {
            switch (value)
            {
                case MeetingCallState.None:
                    return CallState.None;

                case MeetingCallState.EarlyMedia:
                    return CallState.EarlyMedia;

                case MeetingCallState.Connecting:
                    return CallState.Connecting;

                case MeetingCallState.Ringing:
                    return CallState.Ringing;

                case MeetingCallState.Connected:
                    return CallState.Connected;

                case MeetingCallState.LocalHold:
                    return CallState.LocalHold;

                case MeetingCallState.Disconnecting:
                    return CallState.Disconnecting;

                case MeetingCallState.Disconnected:
                    return CallState.Disconnected;

                case MeetingCallState.InLobby:
                    return CallState.InLobby;

                case MeetingCallState.RemoteHold:
                    return CallState.RemoteHold;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
