// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Azure.Communication.Calling.Unity.Rest
{
    public static class Presence
    {

        /// <summary>
        /// Get the signed in user's presence from Microsoft Graph
        /// </summary>
        public static Task<IPresence> Get(
            string authenticationToken)
        {
            QueryBuilder builder = new QueryBuilder("https://graph.microsoft.com/v1.0/me/presence", maxArguments: 0);
            return Client.Get<IPresence, GraphPresence>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get a user's presence from Microsoft Graph
        /// </summary>
        public static Task<IPresence> Get(
            string authenticationToken,
            string userId)
        {
            Client.ValidateString(nameof(userId), userId);

            QueryBuilder builder = new QueryBuilder($"https://graph.microsoft.com/v1.0/users/{userId}/presence", maxArguments: 0);
            return Client.Get<IPresence, GraphPresence>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get all user's presences from Microsoft Graph
        /// </summary>
        ///
        public static Task<string> Get(
            string authenticationToken,
            List<string> userIds)
        {
            int count = 1;
            string url = $"https://graph.microsoft.com/v1.0/$batch";
            string querry = "{\r\n\"requests\": [";
            foreach (var userId in userIds)
            {
                //Client.ValidateString(nameof(userId), userId);
                var querryPart = "{" + $"\r\n\"url\": \"/users/{userId}/presence\",\r\n\"method\": \"GET\",\r\n\"id\": \"{count}\"\r\n" + "}";
                if (count < userIds.Count)
                    querryPart += ",";
                querry += querryPart;
                count++;
            }
            querry += "]\r\n}";

            return Client.Post(
                url, querry,
                authenticationToken);
        }

        /// <summary>
        /// Set the user's presence via the Microsoft Graph
        /// </summary>
        public static Task Set(
            string authenticationToken,
            string applicationId,
            PresenceAvailability availability,
            PresenceActivity activity,
            TimeSpan expirationDuration)
        {
            Client.ValidateString(nameof(applicationId), applicationId);
            Client.ValidateTimeSpan(nameof(expirationDuration), expirationDuration, TimeSpan.FromMinutes(5), TimeSpan.FromHours(4));

            var content = new GraphPresenceInput()
            {
                sessionId = applicationId,
                availability = availability,
                activity = activity,
                expirationDuration = expirationDuration
            };

            QueryBuilder builder = new QueryBuilder($"https://graph.microsoft.com/v1.0/me/presence/setPresence", maxArguments: 0);
            return Client.Request<IPresneceInput, GraphPresenceInput, RestResponse, RestResponse>(
                System.Net.Http.HttpMethod.Post,
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken,
                content);
        }

    }

    public interface IPresence
    {
        string id { get; }

        PresenceAvailability availability { get; }

        PresenceActivity activity { get; }
    }

    public interface IPresneceInput 
    {
        string sessionId { get; }

        PresenceAvailability availability { get; }

        PresenceActivity activity { get; }

        TimeSpan expirationDuration { get; }
    }

    [Serializable]
    public class GraphPresence : RestResponse, IPresence
    {
        public string id { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PresenceAvailability availability { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PresenceActivity activity { get; set; }
    }

    [Serializable]
    public class GraphPresenceInput : RestRequest, IPresneceInput
    {
        public string sessionId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PresenceAvailability availability { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public PresenceActivity activity { get; set; }

        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan expirationDuration { get; set; }
    }

    public enum PresenceAvailability
    {
        Available,
        AvailableIdle,
        Away,
        BeRightBack,
        Busy,
        BusyIdle,
        DoNotDisturb, 
        Offline, 
        PresenceUnknown
    }

    public enum PresenceActivity
    {
        Available, 
        Away, 
        BeRightBack,
        Busy,
        DoNotDisturb, 
        InACall, 
        InAConferenceCall,
        Inactive,
        InAMeeting, 
        Offline, 
        OffWork, 
        OutOfOffice, 
        PresenceUnknown, 
        Presenting, 
        UrgentInterruptionsOnly
    }
}
