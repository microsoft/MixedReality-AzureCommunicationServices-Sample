// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Azure.Communication.Calling.UnityClient;
using System;
using UnityEngine;

namespace Azure.Communication.Calling.Unity
{
    public class TeamsCoordinatesLocator : MeetingLocator
    {
        [SerializeField]
        [Tooltip("")]
        private string threadId;

        /// <summary>
        /// </summary>
        public string ThreadId
        {
            get => threadId;
            set => threadId = value;
        }

        [SerializeField]
        [Tooltip("")]
        private string messageId;

        /// <summary>
        /// </summary>
        public string MessageId
        {
            get => messageId;
            set => messageId = value;
        }

        [SerializeField]
        [Tooltip("")]
        private string organizerId;

        /// <summary>
        /// </summary>
        public Guid OrganizerId
        {
            get
            {
                Guid guid = Guid.Empty;
                Guid.TryParse(organizerId, out guid);
                return guid;
            }
            set => organizerId = value.ToString();
        }

        [SerializeField]
        [Tooltip("")]
        private string tenantId;

        /// <summary>
        /// </summary>
        public Guid TenantId
        {
            get
            {
                Guid guid = Guid.Empty;
                Guid.TryParse(tenantId, out guid);
                return guid;
            }
            set => tenantId = value.ToString();
        }

        /// <summary>
        /// Create the internal representation of a meeting locator that is give the Azure Communications SDK when joining a meeting.
        /// </summary>
        public override JoinMeetingLocator CreateJoinMeetingLocator()
        {
            return new TeamsMeetingCoordinatesLocator(ThreadId, OrganizerId, TenantId, MessageId);
        }

        public override string ToString()
        {
            return $"TeamsCoordinatesLocator({ThreadId}, {OrganizerId}, {TenantId}, {MessageId})";
        }
    }
}