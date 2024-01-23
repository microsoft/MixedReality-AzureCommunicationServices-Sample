// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;

namespace Azure.Communication.Calling.Unity
{
    public interface IDisplayTime
    {
        /// <summary>
        /// The absolute time delta from now to dateTimeOffset
        /// </summary>
        TimeSpan timeDelta { get; }

        /// <summary>
        /// A date time offset from which the display time originates from.
        /// </summary>
        DateTimeOffset dateTimeOffset { get; }

        /// <summary>
        /// The time display for the meeting.
        /// </summary>
        string displayTime { get; }
    }
}