// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Azure.Communication.Calling.Unity
{
    public class PresenceSetter : AuthenticatedOperation
    {
        float _nextRefreshAt = float.MaxValue;
        PresenceAvailability _lastAvailability = PresenceAvailability.PresenceUnknown;
        PresenceActivity _lastActivity = PresenceActivity.PresenceUnknown;

        #region Serializable Fields
        [Header("Presence Settings")]

        [SerializeField]
        [Tooltip("The getter that will load presence data. The setter behaviour will call refresh on this object.")]
        private PresenceGetter presenceGetter = null;

        [SerializeField]
        [Range(5, 240)]
        [Tooltip("The duration in minutes of when the presence will expire.")]
        private int presenceExpirationDuration = 5;

        [Header("Meeting Settings")]

        [SerializeField]
        [Tooltip("The meeting manager used to decide what type of presence should be set.")]
        private MeetingManager meetingManager = null;
        #endregion Serializable Fields

        #region Public Events
        [Header("Events")]

        [SerializeField]
        private PresenceUpdatedEvent presenceUpdated = new PresenceUpdatedEvent();

        public event Action<PresenceSetter, PresenceUpdatedEventArgs> PresenceUpdated;
        #endregion Public Events

        #region Public Properties
        public IPresence Presence { get; private set; }
        #endregion Public Properties

        #region MonoBehaviour Functions
        protected override void Start()
        {
            base.Start();

            if (meetingManager != null)
            {
                meetingManager.StatusChanged += OnMeetingManagerStatusChanged;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (meetingManager != null)
            {
                meetingManager.StatusChanged -= OnMeetingManagerStatusChanged;
            }
        }

        private void Update()
        {
            if (Time.fixedTime >= _nextRefreshAt)
            {
                // avoid another refresh while there is an active refresh
                _nextRefreshAt = float.MaxValue;

                SetPresenceWorker(force: true);
            }
        }
        #endregion MonoBehavior Functions

        #region Protected Functions
        protected override void OnAuthenticated()
        {
            SetPresenceWorker();
        }
        #endregion Protected Functions

        #region Private Functions
        private void OnMeetingManagerStatusChanged(MeetingManager sender, MeetingStatus status)
        {
            SetPresenceWorker();
        }

        private async void SetPresenceWorker(bool force = false)
        {
            var availability = ResolveAvailability();
            var activity = ResolveActivity();

            bool updated = false;
            string token = Token;
            string applicationId = ClientId;
            if (!string.IsNullOrEmpty(token) &&
                !string.IsNullOrEmpty(applicationId) &&
                (force || availability != _lastAvailability || activity != _lastActivity))
            {
                _lastAvailability = availability;
                _lastActivity = activity;

                try
                {
                    Log.Verbose<PresenceSetter>("Updating presence for signed in user.");
                    await Rest.Presence.Set(token, applicationId, availability, activity, TimeSpan.FromMinutes(presenceExpirationDuration));
                    Log.Verbose<PresenceSetter>("Updated for user profile completed.");
                    updated = true;
                }
                catch (Exception ex)
                {
                    Log.Error<PresenceSetter>("Failed to update signed in user's presence. Exception: {0}", ex);
                }
            }


            if (updated)
            {
                Log.Verbose<PresenceSetter>("Updated presence");
                var args = new PresenceUpdatedEventArgs();
                presenceUpdated?.Invoke(args);
                PresenceUpdated?.Invoke(this, args);

                if (presenceGetter != null)
                {
                    presenceGetter.Refresh();
                }
            }

            // refresh presence a minute before expiration
            _nextRefreshAt = Time.fixedTime + ((presenceExpirationDuration - 1) * 60);
        }

        private PresenceAvailability ResolveAvailability()
        {
            if (meetingManager == null)
            {
                return PresenceAvailability.Available;
            }
            else if (meetingManager.Status.CallState == MeetingCallState.InLobby ||
                meetingManager.Status.CallState == MeetingCallState.Connecting ||
                meetingManager.Status.CallState == MeetingCallState.Connected)
            {
                return PresenceAvailability.Busy;
            }
            else
            {
                return PresenceAvailability.Available;
            }
        }

        private PresenceActivity ResolveActivity()
        {
            if (meetingManager == null)
            {
                return PresenceActivity.Available;
            }
            else if (meetingManager.Status.CallState == MeetingCallState.InLobby ||
                meetingManager.Status.CallState == MeetingCallState.Connecting ||
                meetingManager.Status.CallState == MeetingCallState.Connected)
            {
                return PresenceActivity.InACall;
            }
            else
            {
                return PresenceActivity.Available;
            }
        }
        #endregion
    }

    [Serializable]
    public class PresenceUpdatedEvent : UnityEvent<PresenceUpdatedEventArgs>
    { }

    [Serializable]
    public class PresenceUpdatedEventArgs
    {
    }
}
