// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;

namespace Azure.Communication.Calling.Unity
{
    public enum MeetingCallState
    {
        Unknown = 0,
        NoCall,
        EarlyMedia,
        Connecting,
        Ringing,
        Connected,
        LocalHold,
        Disconnecting,
        Disconnected,
        InLobby,
        RemoteHold
    }
}
