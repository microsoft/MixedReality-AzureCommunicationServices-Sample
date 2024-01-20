// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Azure.Communication.Calling.Unity
{
    public class ProfileGetter : AuthenticatedOperation
    {
        #region Serializable Fields
        [Header("User Settings")]

        [SerializeField]
        [Tooltip("The user id to load. If null or empty, the signed in user's profile is loade.")]
        private string id = null;

        /// <summary>
        /// The user id to load. If null or empty, the signed in user's profile is loade.
        /// </summary>
        public string Id
        { 
            get => id;
            set => id = value;
        }
        #endregion Serializable Fields

        #region Public Events
        [Header("Events")]

        [SerializeField]
        private ProfileLoadedEvent profileLoaded = new ProfileLoadedEvent();

        public event Action<ProfileGetter, ProfileLoadedEventArgs> ProfileLoaded;
        #endregion Public Events

        #region Public Properties
        public IUser Profile { get; private set; }
        #endregion Public Properties

        #region Protected Functions
        protected override void OnAuthenticated()
        {
            UpdateProfileWorkerAsync();
        }
        #endregion Protected Functions

        #region Private Functions
        private async void UpdateProfileWorkerAsync()
        {
            IUser user = null;
            string token = Token;
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        Log.Verbose<ProfileGetter>("Requesting profile for signed in user.");
                        user = await User.Get(token);
                    }
                    else
                    {
                        Log.Verbose<ProfileGetter>("Requesting profile for user ({0})", id);
                        user = await User.Get(token, id);
                    }
                    Log.Verbose<ProfileGetter>("Requested for user profile completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<ProfileGetter>("Failed to obtain user profile. Exception: {0}", ex);
                }
            }


            if (user != null)
            {
                Log.Verbose<ProfileGetter>("Loaded profile");
                Profile = user;
                var args = new ProfileLoadedEventArgs(user);
                profileLoaded?.Invoke(args);
                ProfileLoaded?.Invoke(this, args);
            }

        }
        #endregion
    }

    [Serializable]
    public class ProfileLoadedEvent : UnityEvent<ProfileLoadedEventArgs>
    { }

    [Serializable]
    public class ProfileLoadedEventArgs
    {
        public ProfileLoadedEventArgs(IUser user)
        {
            User = user;
        }

        public IUser User { get; private set; }
    }
}
