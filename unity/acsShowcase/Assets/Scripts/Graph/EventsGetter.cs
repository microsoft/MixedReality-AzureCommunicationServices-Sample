// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Azure.Communication.Calling.Unity
{
    public class EventsGetter : AuthenticatedOperation
    {
        #region Serializable Fields
        [Header("Meeting Settings")]

        [SerializeField]
        [Tooltip("The meeting manager used to create new calls")]
        private MeetingManager meetingManager = null;

        [SerializeField]
        [Tooltip("The max duration, in seconds, from now to the start of a primary meeting.")]
        public float maxSecondsDeltaForPrimaryEvent = 15 * 60;

        [Header("Test Settings")]

        [SerializeField]
        private StaticEvent[] staticEvents = new StaticEvent[0];

        #endregion Serializable Fields

        #region Public Events
        [Header("Events")]

        [SerializeField]
        private DisplayTimeChangedEvent displayTimeChanged = new DisplayTimeChangedEvent();

        public event Action<EventsGetter, DisplayTimeChangedEventArgs> DisplayTimeChanged;

        [SerializeField]
        private EventsChangedEvent eventsChanged = new EventsChangedEvent();

        public event Action<EventsGetter, EventsChangedEventArgs> EventsChanged;

        [SerializeField]
        private HasEventsChangedEvent hasEventsChanged = new HasEventsChangedEvent();

        public event Action<EventsGetter, bool> HasEventsChanged;

        [SerializeField]
        private EventsChangedEvent primaryEventsChanged = new EventsChangedEvent();

        public event Action<EventsGetter, EventsChangedEventArgs> PrimaryEventsChanged;

        [SerializeField]
        private HasEventsChangedEvent hasPrimaryEventsChanged = new HasEventsChangedEvent();

        public event Action<EventsGetter, bool> HasPrimaryEventsChanged;
        #endregion Public Events

        #region Public Properties
        /// <summary>
        /// The time display for all the primary events.
        /// </summary>
        public IDisplayTime DisplayTime { get; private set; } = null;

        public IReadOnlyList<IEvent> Events { get; private set; } = null;

        public bool HasEvents => Events != null && Events.Count > 0;

        public IReadOnlyList<IEvent> PrimaryEvents { get; private set; } = null;

        public bool HasPrimaryEvents => PrimaryEvents != null && PrimaryEvents.Count > 0;
        #endregion Public Properties

        #region MonoBehaviour Functions
        protected override void Start()
        {
            base.Start();

            //  Since this value starts out false, raise hasEventsChanged when still false so to update UI.
            if (!HasEvents)
            {
                RaiseHasEventsChanged(HasEvents);
            }

            //  Since this value starts out false, raise hasEventsChanged when still false so to update UI.
            if (!HasPrimaryEvents)
            {
                RaiseHasPrimaryEventsChanged(HasPrimaryEvents);
            }
        }
        #endregion MonoBehaviour Functions

        #region Public Functions
        public void RequestUpdate()
        {
            UpdateEventsWorker();
        }
        #endregion Public Functions

        #region Protected Functions
        protected override void OnAuthenticated()
        {
            UpdateEventsWorker();
        }
        #endregion Protected Functions

        #region Private Functions
        private async void UpdateEventsWorker()
        {
            var today = DateTimeOffset.Now;
            var startTimeFilter = new DateTimeOffset(year: today.Year, month: today.Month, day: today.Day, hour: 0, minute: 0, second: 0, millisecond: 0, today.Offset);
            var endTimeFilter = new DateTimeOffset(year: today.Year, month: today.Month, day: today.Day, hour: 23, minute: 59, second: 59, millisecond: 999, today.Offset);

            DisplayTime = new DayMonthDisplayTime(startTimeFilter);
            var primaryDisplayTimeArgs = new DisplayTimeChangedEventArgs(DisplayTime);
            DisplayTimeChanged?.Invoke(this, primaryDisplayTimeArgs);
            displayTimeChanged?.Invoke(primaryDisplayTimeArgs);

            IEvents events = null;
            string token = Token;
            if (!string.IsNullOrEmpty(token))
            {
                Log.Verbose<EventsGetter>("Requesting upcoming events from the Microsoft Graph...");
                try
                {
                    events = await Rest.Events.GetOnlineMeetings(token, startTimeFilter, endTimeFilter);
                    Log.Verbose<EventsGetter>("Requested upcoming events completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<EventsGetter>("Failed to obtain list of events. Exception: {0}", ex);
                }
            }
            else
            {
                Log.Verbose<EventsGetter>("Unable to request events from the Microsoft Graph, as the authentication access token is empty.");
            }

            var listEvents = new List<IEvent>();
            var listPrimaryEvents = new List<IEvent>();
            var closestMeetingDelta = TimeSpan.MaxValue;

            if (staticEvents != null && staticEvents.Length > 0)
            {
                Log.Verbose<PeopleGetter>("Adding static test events ({0})", staticEvents.Length);
                for (int i = 0; i < staticEvents.Length; i++)
                {
                    var staticEvent = staticEvents[i];
                    listEvents.Add(new EventWithActions(meetingManager, new Rest.GraphEvent()
                    {
                        subject = staticEvent.subject,
                        onlineMeeting = new GraphEventOnlineMeeting()
                        {
                            joinUrl = staticEvent.joinUrl
                        }
                    }));
                }
            }

            if (events?.value != null)
            {
                Log.Verbose<EventsGetter>("Received events ({0})", events.value.Length);
                foreach (var @event in events.value)
                {
                    Log.Verbose<EventsGetter>("    * {0}", @event.subject);
                    var eventWithActions = new EventWithActions(meetingManager, @event);
                    listEvents.Add(eventWithActions);

                    // only save one primary meeting for now
                    var timeDelta = eventWithActions.timeDelta;
                    if (timeDelta < closestMeetingDelta && timeDelta.TotalSeconds <= maxSecondsDeltaForPrimaryEvent)
                    {
                        listPrimaryEvents.Clear();
                        listPrimaryEvents.Add(eventWithActions);
                    }
                }
            }

            bool hadEvents = HasEvents;
            bool hadPrimaryEvents = HasPrimaryEvents;

            Events = listEvents.AsReadOnly();
            var args = new EventsChangedEventArgs(Events);
            EventsChanged?.Invoke(this, args);
            eventsChanged?.Invoke(args);

            PrimaryEvents = listPrimaryEvents.AsReadOnly();
            var primaryArgs = new EventsChangedEventArgs(PrimaryEvents);
            PrimaryEventsChanged?.Invoke(this, primaryArgs);
            primaryEventsChanged?.Invoke(primaryArgs);

            if (hadEvents != HasEvents)
            {
                RaiseHasEventsChanged(HasEvents);
            }

            if (hadPrimaryEvents != HasPrimaryEvents)
            {
                RaiseHasPrimaryEventsChanged(HasPrimaryEvents);
            }
        }

        private void RaiseHasEventsChanged(bool value)
        {
            HasEventsChanged?.Invoke(this, value);
            hasEventsChanged?.Invoke(value);
        }

        private void RaiseHasPrimaryEventsChanged(bool value)
        {
            HasPrimaryEventsChanged?.Invoke(this, value);
            hasPrimaryEventsChanged?.Invoke(value);
        }
        #endregion Private Functions
    }

    [Serializable]
    public class EventsChangedEvent : UnityEvent<EventsChangedEventArgs>
    { }

    [Serializable]
    public class EventsChangedEventArgs
    {
        public EventsChangedEventArgs(IReadOnlyList<IEvent> events)
        {
            Events = events;
        }

        public IReadOnlyList<IEvent> Events { get; private set; }
    }

    [Serializable]
    public class DisplayTimeChangedEvent : UnityEvent<DisplayTimeChangedEventArgs>
    { }

    [Serializable]
    public class DisplayTimeChangedEventArgs
    {
        public DisplayTimeChangedEventArgs(IDisplayTime displayTime)
        {
            DisplayTime = displayTime;
        }

        public IDisplayTime DisplayTime { get; private set; }
    }

    [Serializable]
    public class HasEventsChangedEvent : UnityEvent<bool>
    { }

    [Serializable]
    public struct StaticEvent
    {
        public string subject;
        public string joinUrl;
    }
}