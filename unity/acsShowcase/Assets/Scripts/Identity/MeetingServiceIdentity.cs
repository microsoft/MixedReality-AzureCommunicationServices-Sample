// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

namespace Azure.Communication.Calling.Unity
{
    /// <summary>
    /// An internal class representing an identity to use with the Azure Communication Services.
    /// </summary>
    public class MeetingServiceIdentity
    {
        internal MeetingServiceIdentity(string endpoint, string communcationAccessToken, string graphAccessToken, bool isGuest, string displayName = null)
        {
            Endpoint = endpoint;
            CommunicationAccessToken = communcationAccessToken;
            GraphAccessToken = graphAccessToken;
            LocalParticipant = MeetingParticipant.CreateLocal(displayName, isGuest);
        }

        /// <summary>
        /// Get the local pariticipant information
        /// </summary>
        internal MeetingParticipant LocalParticipant { get; }

        /// <summary>
        /// Get the Azure Communication Service's endpoint
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// The authentication access token for the Azure Communication Services. This is the user access token created by the ACS identity services, not the AAD access token.
        /// </summary>
        public string CommunicationAccessToken { get; }

        /// <summary>
        /// The authentication access token for the Microsoft Graph services.
        /// </summary>
        public string GraphAccessToken { get; }
    }
}
