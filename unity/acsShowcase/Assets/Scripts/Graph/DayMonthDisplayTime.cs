// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;

namespace Azure.Communication.Calling.Unity
{
    internal class DayMonthDisplayTime : IDisplayTime
    {
        public DayMonthDisplayTime(DateTimeOffset time)
        {
            dateTimeOffset = time;
        }

        public TimeSpan timeDelta => (DateTimeOffset.UtcNow - dateTimeOffset).Duration();

        public DateTimeOffset dateTimeOffset { get; private set; }

        public string displayTime
        {
            get 
            {
                // Not doing dateTimeOffset.ToString("dddd, MMMM M") to avoid localization failures for the month name and month day combination.
                return $"{(dateTimeOffset.ToString("dddd"))}, {(dateTimeOffset.ToString("M"))}";
            }
        }
    }
}
