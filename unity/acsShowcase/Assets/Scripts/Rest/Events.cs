// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Azure.Communication.Calling.Unity.Rest
{
    public static class Events
    {
        /// <summary>
        /// Get calendar events within the given start and end times
        /// </summary>
        public static Task<IEvents> Get(
            string authenticationToken,
            DateTimeOffset startTime,
            DateTimeOffset endTime)
        {
            QueryBuilder builder = new QueryBuilder("https://graph.microsoft.com/v1.0/me/events", maxArguments: 3);
            builder.InsertArgument("startdatetime", startTime.ToRfc3339String());
            builder.InsertArgument("enddatetime", endTime.ToRfc3339String());
            builder.InsertArgument("$select", "subject,organizer,onlineMeeting,start,end,onlineMeetingProvider,attendees");
            return Client.Get<IEvents, GraphEvents>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get calendar events within the given start and end times, and filter for only online meetings.
        /// </summary>
        public static Task<IEvents> GetOnlineMeetings(
            string authenticationToken,
            DateTimeOffset startTime,
            DateTimeOffset endTime)
        {
            QueryBuilder builder = new QueryBuilder("https://graph.microsoft.com/v1.0/me/calendarview", maxArguments: 5);
            builder.InsertArgument("startdatetime", startTime.ToRfc3339String());
            builder.InsertArgument("enddatetime", endTime.ToRfc3339String());
            builder.InsertArgument("$expand", "singleValueExtendedProperties($filter=id eq 'String {00020329-0000-0000-C000-000000000046} Name SkypeTeamsMeetingUrl')");
            builder.InsertArgument("$filter", "singleValueExtendedProperties/Any(ep: ep/id eq 'String {00020329-0000-0000-C000-000000000046} Name SkypeTeamsMeetingUrl' and startswith(ep/value, 'http'))");
            builder.InsertArgument("$select", "subject,organizer,onlineMeeting,start,end,onlineMeetingProvider,attendees");
            return Client.Get<IEvents, GraphEvents>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }
    }

    public interface IEvents
    {
        IEvent[] value { get; }
    }

    public interface IEvent
    {
        string id { get; }

        string subject { get; }

        IEventOrganizer organizer { get; }

        IEventOnlineMeeting onlineMeeting { get; }

        IEventDateTime start { get; }

        IEventDateTime end { get; }

        EventOnlineMeetingProviderType onlineMeetingProvider { get; }

        IEventAttendees attendees { get; }
    }

    public interface IEventOrganizer
    {
        IEventEmailAddress emailAddress { get; }
    }

    public interface IEventEmailAddress
    {
        string name { get; }

        string address { get; }
    }


    public interface IEventOnlineMeeting
    {
        string joinUrl { get; }
    }

    public interface IEventDateTime
    {
        DateTimeOffset dateTimeOffset { get; }
    }

    public interface IEventAttendees
    {
        IEventAttendee[] value { get; }
    }

    public interface IEventAttendee
    {
        IEventEmailAddress emailAddress { get; }

        IEventResponseStatus status { get; }

        EventAttendeeType type { get; }
    }

    public interface IEventResponseStatus
    {
        EventResponse response { get; }

        DateTimeOffset timeOffset { get; }
    }

    public enum EventAttendeeType
    {
        required, 
        optional, 
        resource
    }

    public enum EventResponse
    {
        none,
        organizer, 
        tentativelyAccepted, 
        accepted, 
        declined, 
        notResponded
    }

    public enum EventOnlineMeetingProviderType
    {
        unknown, 
        teamsForBusiness, 
        skypeForBusiness, 
        skypeForConsumer
    }

    [Serializable]
    public class GraphEvents : RestResponse, IEvents
    {
        [JsonProperty("value", ItemConverterType = typeof(ConcreteConverter<GraphEvent>))]
        public IEvent[] value { get; set; } = new GraphEvent[0];
    }

    [Serializable]
    public class GraphEvent : RestResponse, IEvent
    {
        public string id { get; set; } = string.Empty;

        public string subject { get; set; } = string.Empty;

        [JsonConverter(typeof(ConcreteConverter<GraphEventOrganizer>))]
        public IEventOrganizer organizer { get; set; } = null;

        [JsonConverter(typeof(ConcreteConverter<GraphEventOnlineMeeting>))]
        public IEventOnlineMeeting onlineMeeting { get; set; } = null;

        [JsonConverter(typeof(ConcreteConverter<GraphEventDateTime>))]
        public IEventDateTime start { get; set; } = null;

        [JsonConverter(typeof(ConcreteConverter<GraphEventDateTime>))]
        public IEventDateTime end { get; set; } = null;

        [JsonConverter(typeof(StringEnumConverter))]
        public EventOnlineMeetingProviderType onlineMeetingProvider { get; set; } = EventOnlineMeetingProviderType.unknown;

        [JsonIgnore]
        public IEventAttendees attendees { get; private set; } = null;

        [JsonProperty("attendees", ItemConverterType = typeof(ConcreteConverter<GraphEventAttendee>))]
        public IEventAttendee[] adttendeesArray
        {
            get => attendees?.value;

            set => attendees = new GraphEventAttendees(value);
        }
    }

    [Serializable]
    public class GraphEventOrganizer : IEventOrganizer
    {
        [JsonConverter(typeof(ConcreteConverter<GraphEventEmailAddress>))]
        public IEventEmailAddress emailAddress { get; set; } = null;
    }

    [Serializable]
    public class GraphEventEmailAddress : IEventEmailAddress
    {
        public string name { get; set; } = string.Empty;

        public string address { get; set; } = string.Empty;
    }

    [Serializable]
    public class GraphEventDateTime : IEventDateTime
    {
        DateTimeOffset _utc = DateTimeOffset.MinValue;

        public DateTime dateTime { get; set; } = DateTime.MinValue;

        public string timeZone { get; set; } = string.Empty;

        [JsonIgnore]
        public DateTimeOffset dateTimeOffset
        {
            get
            {
                if (_utc == DateTimeOffset.MinValue && !string.IsNullOrEmpty(timeZone))
                {
                    TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                    _utc = new DateTimeOffset(dateTime, timeZoneInfo.BaseUtcOffset);
                }
                return _utc;
            }
        }
    }

    [Serializable]
    public class GraphEventOnlineMeeting : IEventOnlineMeeting
    {
        public string joinUrl { get; set; } = string.Empty;
    }

    [Serializable]
    public class GraphEventAttendees : IEventAttendees
    {
        public GraphEventAttendees(IEventAttendee[] value)
        {
            this.value = value;
        }

        public IEventAttendee[] value { get; } = null;
    }

    [Serializable]
    public class GraphEventAttendee : IEventAttendee
    {
        [JsonConverter(typeof(ConcreteConverter<GraphEventEmailAddress>))]
        public IEventEmailAddress emailAddress { get; set; } = null;

        [JsonConverter(typeof(ConcreteConverter<GraphEventResponseStatus>))]
        public IEventResponseStatus status { get; set; } = null;

        [JsonConverter(typeof(StringEnumConverter))]
        public EventAttendeeType type { get; set; } = EventAttendeeType.optional;
    }

    [Serializable]
    public class GraphEventResponseStatus : IEventResponseStatus
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public EventResponse response { get; set; } = EventResponse.none;
        
 
        public DateTime time { get; } = DateTime.MinValue;

        [JsonIgnore]
        public DateTimeOffset timeOffset => time;
    }
}
