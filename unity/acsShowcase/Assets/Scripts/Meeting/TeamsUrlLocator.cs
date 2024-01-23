// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.UnityClient;
using UnityEngine;
using System;

namespace Azure.Communication.Calling.Unity
{
    public class TeamsUrlLocator : MeetingLocator
    {
        private string _chatThreadId;

        public TeamsUrlLocator(string url)
        {
            this.url = url;
        }

        [SerializeField]
        [Tooltip("The Teams URL to join.")]
        private string url;

        /// <summary>
        /// The Teams URL to join.
        /// </summary>
        public string Url
        {
            get => url;
            set
            {
                url = value;
                _chatThreadId = null;
            }
        }

        /// <summary>
        /// Create the internal representation of a meeting locator that is give the Azure Communications SDK when joining a meeting.
        /// </summary>
        public override JoinMeetingLocator CreateJoinMeetingLocator()
        {
            if (string.IsNullOrEmpty(url) || url.Trim().Length == 0 || !url.StartsWith("http"))
            {
                throw new ArgumentException("Teams link is invalid.");
            }

            return new TeamsMeetingLinkLocator(Url);
        }

        public override string ToString()
        {
            return $"TeamsUrlLocator({Url})";
        }
    }
}
