// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Azure.Communication.Calling.Unity
{ 
    public class PresenceGetter : AuthenticatedOperation
    {
        #region Serializable Fields
        [Header("User Settings")]

        [SerializeField]
        [Tooltip("The user id to load. If null or empty, the signed in user's profile is loade.")]
        private string id = null;

        /// <summary>
        /// The user id to load. If null or empty, the signed in user's presence will be loaded.
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
        private PresenceLoadedEvent presenceLoaded = new PresenceLoadedEvent();

        public event Action<PresenceGetter, PresenceLoadedEventArgs> PresenceLoaded;
        public static event Action<List<StaticUserProfile>> OnPresenceFullyLoaded;
        #endregion Public Events

        #region Public Properties
        public IPresence Presence { get; private set; }
        #endregion Public Properties

        #region Protected Functions
        protected override void OnAuthenticated()
        {
            UpdatePresenceAsyncWorker();
        }
        #endregion Protected Functions

        #region Public Functions
        public void Refresh()
        {
            UpdatePresenceAsyncWorker();
        }
        #endregion Public Function

        #region Private Functions
        private async void UpdatePresenceAsyncWorker()
        {
            IPresence presence = null;
            string token = Token;
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        Log.Verbose<PresenceGetter>("Requesting presence for signed in user.");
                        presence = await Rest.Presence.Get(token);
                    }
                    else
                    {
                        Log.Verbose<PresenceGetter>("Requesting presence for user ({0})", id);
                        presence = await Rest.Presence.Get(token, id);
                    }
                    Log.Verbose<PresenceGetter>("Requested for user presence completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<PresenceGetter>("Failed to obtain user's presence. Exception: {0}", ex);
                }
            }


            var args = new PresenceLoadedEventArgs(null);
            if (presence != null)
            {
                Log.Verbose<PresenceGetter>("Loaded presence");
                Presence = presence;
                args = new PresenceLoadedEventArgs(presence);
            }
            presenceLoaded?.Invoke(args);
            PresenceLoaded?.Invoke(this, args);
        }

        private PresenceAvailability GetPresence(string presence)
        {
            switch (presence)
            {
                case "available":
                    return PresenceAvailability.Available;
                case "availableidle":
                    return PresenceAvailability.AvailableIdle;
                case "away":
                    return PresenceAvailability.Away;
                case "berightback":
                    return PresenceAvailability.BeRightBack;
                case "busy":
                    return PresenceAvailability.Busy;
                case "busyidle":
                    return PresenceAvailability.BusyIdle;
                case "donotdisturb":
                    return PresenceAvailability.DoNotDisturb;
                case "offline":
                    return PresenceAvailability.Offline;
                case "inameeting":
                    return PresenceAvailability.Busy;
                default:
                    return PresenceAvailability.PresenceUnknown;
            }
        }
        #endregion Private Functions

        #region Public Functions
        public async Task<List<StaticUserProfile>> UpdatePresenceAsyncWorker(List<StaticUserProfile> userProfiles)
        {
            string data = null;
            var deserializedData = new ReturnedPresenceData();
            string token = Token;
            var tempUserProfiles = new List<StaticUserProfile>();
            var ids = new List<string>(); 
            foreach (var profile in userProfiles)
            {
                ids.Add(profile.Id);
            }
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    Log.Verbose<PhotoGetter>("Requesting presences for all users");
                    
                    data = await Rest.Presence.Get(token, ids);
                    deserializedData = Newtonsoft.Json.JsonConvert.DeserializeObject<ReturnedPresenceData>(data);
                    Log.Verbose<PhotoGetter>("Request for user presences completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<PhotoGetter>("Failed to obtain user presences. Exception: {0}", ex);
                }
            }

            if (deserializedData.responses != null && deserializedData.responses.Count() > 0)
            {
                foreach (var response in deserializedData.responses)
                { 
                    var count = response.id - 1; 
                    if(response.body.activity != null)
                        tempUserProfiles.Add(new StaticUserProfile(userProfiles[count].Id, userProfiles[count].DisplayName, userProfiles[count].Email, userProfiles[count].Icon, GetPresence(response.body.activity.ToLower())));  
                    else
                        tempUserProfiles.Add(new StaticUserProfile(userProfiles[count].Id, userProfiles[count].DisplayName, userProfiles[count].Email, userProfiles[count].Icon, PresenceAvailability.PresenceUnknown)); 
                }
            }
            OnPresenceFullyLoaded?.Invoke(tempUserProfiles);
            return tempUserProfiles;
        }
        #endregion Public Functions
    }

    [Serializable]
    public class PresenceLoadedEvent : UnityEvent<PresenceLoadedEventArgs>
    { }

    [Serializable]
    public class PresenceLoadedEventArgs
    {
        public PresenceLoadedEventArgs(IPresence presence)
        {
            Presence = presence;
        }

        public IPresence Presence { get; private set; }
    }
    public struct PresenceBody
    {
        [JsonProperty(PropertyName = "activity")]
        public string activity { get; set; } 
    }
    public struct PresenceResponses
    {
        [JsonProperty(PropertyName = "id")]
        public int id { get; set; }
        [JsonProperty(PropertyName = "body")]
        public PresenceBody body { get; set; }
    }
    public struct ReturnedPresenceData
    {
        [JsonProperty(PropertyName = "responses")]
        public IEnumerable<PresenceResponses> responses { get; set; }
    }
}
