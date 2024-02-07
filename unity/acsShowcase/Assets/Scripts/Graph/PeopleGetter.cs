// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.Unity.Rest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Azure.Communication.Calling.Unity
{
    public class PeopleGetter : AuthenticatedOperation
    {
        #region Serializable Fields
        [Header("Meeting Settings")]

        [SerializeField]
        [Tooltip("The meeting manager used to create new calls")]
        private MeetingManager meetingManager = null;

        [Header("Test Settings")]

        [SerializeField]
        private StaticPerson[] staticPeople = new StaticPerson[0];
        #endregion Serializable Fields

        #region Public Events
        [Header("Events")]

        [SerializeField]
        private PeopleChangedEvent peopleChanged = new PeopleChangedEvent();

        public static event Action<PeopleGetter, PeopleChangedEventArgs> PeopleChanged;
        public static event Action<string> SendToken;
        public delegate void GetPeopleHandler(IUsers allUsers);
        
        
        #endregion Public Events

        #region Public Properties
        public IReadOnlyList<IUser> People { get; private set; }
        #endregion Public Properties

        
        #region Protected Functions
        protected override void OnAuthenticated()
        {
            UpdatePeopleWorker();
        }
        #endregion Protected Functions

        #region Public Functions
        public void RequestUpdate()
        {
            UpdatePeopleWorker();
        }

        public async void GetPeopleWorker(List<string> emailList, GetPeopleHandler handler)
        {
            IUsers people = null;
            string token = Token;
            if (!string.IsNullOrEmpty(token))
            {
                Log.Verbose<PeopleGetter>("Requesting for people from the Microsoft Graph...");
                try
                {
                    people = await Rest.People.Get(token, emailList);
                    Log.Verbose<PeopleGetter>("Requested for people completed.");
                    handler(people);
                }
                catch (Exception ex)
                {
                    Log.Error<PeopleGetter>("Failed to obtain list of people. Exception: {0}", ex);
                }
            }
        }
        #endregion Public Functions

        #region Private Functions
        private async void UpdatePeopleWorker()
        {
            IUsers people = null;
            string token = Token;
            SendToken?.Invoke(token);
            if (!string.IsNullOrEmpty(token))
            {
                Log.Verbose<PeopleGetter>("Requesting for people from the Microsoft Graph...");
                try
                {
                    people = await Rest.People.Get(token);
                    Log.Verbose<PeopleGetter>("Requested for people completed.");
                }
                catch (Exception ex)
                {
                    Log.Error<PeopleGetter>("Failed to obtain list of people. Exception: {0}", ex);
                }
            }

            var listPeople = new List<UserWithActions>();
            if (staticPeople != null)
            {
                Log.Verbose<PeopleGetter>("Adding static test people ({0})", staticPeople.Length);
                for (int i = 0; i < staticPeople.Length; i++)
                {
                    var staticPerson = staticPeople[i];
                    Log.Verbose<PeopleGetter>("    * {0}", staticPerson.displayName);
                    listPeople.Add(new UserWithActions(AuthenticationRequest, meetingManager, new GraphPerson()
                    {
                        id = staticPerson.id,
                        userPrincipalName = staticPerson.userPrincipalName,
                        displayName = staticPerson.displayName,
                        internalType = staticPerson.type
                    }));
                }
            }

            if (people?.value != null)
            {
                Log.Verbose<PeopleGetter>("Received people ({0})", people.value.Length);
                foreach (var person in people.value)
                {
                    Log.Verbose<PeopleGetter>("    * {0}", person.displayName);
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

    [Serializable]
    public struct StaticPerson
    {
        public string displayName;
        public string userPrincipalName;
        public string id;
        public InternalUserType type; 
    }

}
