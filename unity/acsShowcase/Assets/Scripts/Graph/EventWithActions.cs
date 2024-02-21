// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using System;
using System.Text;

namespace Azure.Communication.Calling.Unity
{
    internal class EventWithActions : IEvent, IEventActions, IDisplayTime
    {
        private MeetingManager _meetingManager = null;

        internal EventWithActions(MeetingManager meetingManager, IEvent @event)
        {
            _meetingManager = meetingManager;
            inner = @event;
            start = new EventWithActionsDateTime(inner.start);
            end = new EventWithActionsDateTime(inner.end);
            attendees = new EventWithActionAttendees(inner.attendees);
        }

        public IEvent inner { get; private set; }

        public string subject
        {
            get
            {
                string name = inner?.subject;
                if (string.IsNullOrEmpty(name))
                {
                    name = "Unknown GraphEvent";
                }
                return name;
            }
        }

        public string id => inner?.id;

        public IEventOrganizer organizer => inner?.organizer;

        public IEventOnlineMeeting onlineMeeting => inner?.onlineMeeting;

        public EventOnlineMeetingProviderType onlineMeetingProvider => inner?.onlineMeetingProvider ?? EventOnlineMeetingProviderType.unknown;


        public IEventDateTime start { get; }

        public IEventDateTime end { get; }

        public IEventAttendees attendees { get; }

        /// <summary>
        /// The time delta from now to when this meeting starts or when the meeting ended to now.
        /// </summary>
        public TimeSpan timeDelta
        {
            get
            {
                return GetTimeDelta(DateTimeOffset.UtcNow);
            }
        }

        /// <summary>
        /// A date time offset from which the display time originates from.
        /// </summary>
        public DateTimeOffset dateTimeOffset
        {
            get
            {
                return GetClosestDateTimeOffset(DateTimeOffset.UtcNow);
            }
        }

        /// <summary>
        /// The time display for the meeting.
        /// </summary>
        public string displayTime
        { 
            get
            {
                var now = DateTimeOffset.Now;
                if (now >= start.dateTimeOffset && now <= end.dateTimeOffset)
                {
                    return "Now";
                }

                var delta = (now - start.dateTimeOffset).Duration();
                if (now <= start.dateTimeOffset && delta < TimeSpan.FromMinutes(45))
                {
                    if (delta.Minutes < 1)
                    {
                        return "Now";
                    }
                    else if (delta.Minutes == 1)
                    {
                        return "Starts in a minute";
                    }
                    else
                    {
                        return $"Starts in {delta.Minutes} minutes";
                    }
                }

                if (delta.TotalDays >= 1)
                {
                    return start.dateTimeOffset.LocalDateTime.ToString("f");
                }
                else
                {                 
                    return start.dateTimeOffset.LocalDateTime.ToString("t");
                }
            }
        }

        public void Join()
        {
            string joinUrl = inner.onlineMeeting?.joinUrl;
            if (string.IsNullOrEmpty(joinUrl))
            {
                Log.Error<EventWithActions>("Unable to join event, join url == null or empty ({0})", subject);
            }
            else if (_meetingManager == null)
            {

                Log.Error<EventWithActions>("Unable to join event, meeting manager == null ({0})", subject);
            }
            else
            {
                _meetingManager.Join(new TeamsUrlLocator(joinUrl));
            }
        }

        private DateTimeOffset GetClosestDateTimeOffset(DateTimeOffset now)
        {
            var fromStartDelta = GetTimeDuration(start, now);
            var fromEndDelta = GetTimeDuration(end, now);

            if (fromStartDelta < fromEndDelta)
            {
                return start.dateTimeOffset;
            }
            else
            {
                return end.dateTimeOffset;
            }
        }

        private TimeSpan GetTimeDelta(DateTimeOffset now)
        {
            if (now >= start.dateTimeOffset && now <= end.dateTimeOffset)
            {
                return TimeSpan.Zero;
            }
            else
            {
                return Min(GetTimeDuration(start, now), GetTimeDuration(end, now));
            }
        }

        private TimeSpan GetTimeDuration(IEventDateTime time, DateTimeOffset now)
        {
            return (now - time.dateTimeOffset).Duration();
        }

        private TimeSpan Min(TimeSpan timeSpan1, TimeSpan timeSpan2)
        {
            if (timeSpan1 < timeSpan2)
            {
                return timeSpan1;
            }
            else
            {
                return timeSpan2;
            }
        }
    }

    internal class EventWithActionsDateTime : IEventDateTime
    {
        internal EventWithActionsDateTime(IEventDateTime eventDateTime)
        {
            inner = eventDateTime;
        }

        public IEventDateTime inner { get; private set; }

        public DateTimeOffset dateTimeOffset => inner?.dateTimeOffset ?? DateTimeOffset.Now;

        public override string ToString()
        {
            return dateTimeOffset.LocalDateTime.ToString("f");
        }
    }

    internal class EventWithActionAttendees : IEventAttendees
    {
        internal EventWithActionAttendees(IEventAttendees attendees)
        {
            inner = attendees;
        }

        public IEventAttendees inner { get; private set; }

        public IEventAttendee[] value => inner?.value;

        public override string ToString()
        {
            if (inner == null || inner.value == null || inner.value.Length == 0)
            {
                return "Nobody";
            }
            else
            {
                StringBuilder result = new StringBuilder();
                int maxNames = 3;
                int maxAttendees = inner.value.Length;
                for (int i = 0; i < maxNames && i < maxAttendees; i++)
                {
                    string name = inner.value[i].emailAddress?.name;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = "Unknown";
                    }

                    if (i != 0)
                    {
                        result.Append(", ");
                    }

                    result.Append(name);
                }

                int remainingNames = maxAttendees - maxNames;
                if (remainingNames > 0)
                {
                    result.Append(", +");
                    result.Append(remainingNames);
                }

                return result.ToString();
            }

        }
    }

}