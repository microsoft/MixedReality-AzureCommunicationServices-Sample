// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;

namespace Azure.Communication.Calling.Unity
{
    public enum MeetingCallState
    {
        None = 0,
        EarlyMedia = 1,
        Connecting = 3,
        Ringing = 4,
        Connected = 5,
        LocalHold = 6,
        Disconnecting = 7,
        Disconnected = 8,
        InLobby = 9,
        RemoteHold = 10
    }
}
