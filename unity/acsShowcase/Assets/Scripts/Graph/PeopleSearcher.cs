// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.UI.DefaultControls;

namespace Azure.Communication.Calling.Unity
{
    public class PeopleSearcher : AuthenticatedOperation
    {
        private bool _queryUpdateNeeded = false;

        #region Serializable Fields
        [Header("Search Settings")]

        [SerializeField]
        [Tooltip("The name prefix to search for.")]
        private string query = null;

        /// <summary>
        /// Get or set the name prefix to search for.
        /// </summary>
        public string Query
        {
            get => query;

            set
            {
                if (query != value)
                {
                    query = value;
                    InvalidateQueryString();
                }
            }
        }

        [SerializeField]
        [Tooltip("The delay between each search query update.")]
        private float queryDelay = 5.0f;

        [Header("Meeting Settings")]

        [SerializeField]
        [Tooltip("The meeting manager used to create new calls")]
        private MeetingManager meetingManager = null;
        #endregion Serializable Fields

        #region Public Events
        [Header("Events")]

        [SerializeField]
        private PeopleChangedEvent peopleChanged = new PeopleChangedEvent();

        public event Action<PeopleSearcher, PeopleChangedEventArgs> PeopleChanged;
        public static event Action<List<StaticUserProfile>> OnSearchComplete;
        #endregion Public Events

        #region Public Properties
        public IReadOnlyList<IUser> People { get; private set; }
        #endregion Public Properties

        #region MonoBehaviour Functions
        private void OnEnable()
        {
            StartCoroutine(SearchUpdater());
        }
        #endregion MonoBehavior Functions

        #region Protected Functions
        protected override void OnAuthenticated()
        {
            InvalidateQueryString();
        }
        #endregion Protected Functions

        #region Public Functions
        public void RequestUpdate()
        {
            UpdateSearchWorker();
        }

        public async void SearchForUsers(string querry)
        {
            IUsers searchResultsDeserialized = null;
            string searchResults = null;
            var deserializedData = new SearchResults();
            string token = Token;
            var tempUserProfiles = new List<StaticUserProfile>();
            int hitCount = 0;
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    Log.Verbose<PhotoGetter>("Requesting presences for all users");
                    searchResults = await Rest.People.Search2(token, querry);
                    deserializedData = Newtonsoft.Json.JsonConvert.DeserializeObject<SearchResults>(searchResults);
                    Log.Verbose<PhotoGetter>("Request for user presences completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<PhotoGetter>("Failed to obtain user presences. Exception: {0}", ex);
                }
            }

            if (deserializedData.value != null && deserializedData.value.Count() > 0 && deserializedData.value.FirstOrDefault().hitsContainers != null
                 && deserializedData.value.FirstOrDefault().hitsContainers.Count() > 0 && deserializedData.value.FirstOrDefault().hitsContainers.FirstOrDefault().hits != null)
            {
                var hits = deserializedData.value.FirstOrDefault().hitsContainers.FirstOrDefault().hits;
                foreach (var hit in hits)
                {
                    var hitResponse = hit.resource;  
                    tempUserProfiles.Add(new StaticUserProfile(hitResponse.id, hitResponse.displayName, hitResponse.email, null, PresenceAvailability.PresenceUnknown));
                }
            }
            OnSearchComplete?.Invoke(tempUserProfiles);

        }
        #endregion Public Functions

        #region Private Functions
        private void InvalidateQueryString()
        {
            Log.Verbose<PeopleSearcher>("Invalidated query string.");
            _queryUpdateNeeded = true;
        }

        private IEnumerator SearchUpdater()
        {
            Log.Verbose<PeopleSearcher>("Starting search updated.");
            while (isActiveAndEnabled)
            {
                if (_queryUpdateNeeded)
                {
                    _queryUpdateNeeded = false;
                    UpdateSearchWorker();
                }

                yield return new WaitForSeconds(queryDelay);
            }
        }

        private async void UpdateSearchWorker()
        {
            IUsers people = null;
            string token = Token;
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(query))
            {
                Log.Verbose<PeopleSearcher>("Searching for people from the Microsoft Graph ({0})...", query);
                try
                {
                    people = await Rest.People.Search(token, query);
                    Log.Verbose<PeopleSearcher>("Search for people completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<PeopleSearcher>("Failed to search for people. Exception: {0}", ex);
                }
            }

            var listPeople = new List<IUser>();

            if (people?.value != null)
            {
                Log.Verbose<PeopleSearcher>("Found people ({0})", people.value.Length);
                foreach (var person in people.value)
                {
                    Log.Verbose<PeopleSearcher>("    * {0}", person.displayName);
                    listPeople.Add(new UserWithActions(AuthenticationRequest, meetingManager, person));
                }
            }

            People = listPeople.AsReadOnly();
            var args = new PeopleChangedEventArgs(People);
            PeopleChanged?.Invoke(this, args);
            peopleChanged?.Invoke(args);
        }
        #endregion Private Functions
    }
    public struct SearchResults
    {
        [JsonProperty(PropertyName = "value")]
        public IEnumerable<Value> value;
    }
    public struct Value
    {
        [JsonProperty(PropertyName = "hitsContainers")]
        public IEnumerable<HitsContainer> hitsContainers;
    }
    public struct HitsContainer
    {
        [JsonProperty(PropertyName = "hits")]
        public IEnumerable<Hit> hits;
    }
    public struct Hit
    { 
        [JsonProperty(PropertyName = "resource")]
        public Resource resource;
    }
    public struct Resource
    {
        [JsonProperty(PropertyName = "id")]
        public string id;
        [JsonProperty(PropertyName = "DisplayName")]
        public string displayName;
        [JsonProperty(PropertyName = "userPrincipalName")]
        public string email;
    }
}
