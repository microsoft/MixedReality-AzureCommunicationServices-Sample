// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Azure.Communication.Calling.Unity.Rest
{
    public class OnlineMeeting
    {
        /// <summary>
        /// Get the details of an online meeting from the Microsoft Graph.
        /// </summary>
        public static Task<IOnlineMeetingResponse> Get(
            string authenticationToken,
            string joinWebUrl)
        {
            Client.ValidateString(nameof(joinWebUrl), joinWebUrl);

            QueryBuilder builder = new QueryBuilder($"https://graph.microsoft.com/v1.0/me/onlineMeetings", maxArguments: 1);
            builder.InsertArgument("$filter", $"JoinWebUrl%20eq%20'{joinWebUrl}'");

            return Client.Get<IOnlineMeetingResponse, GraphOnlineMeetingResponse>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }
        //Update attendee list
        public static Task<string> Patch(
            string authenticationToken,
            string url,string meetingID,
            string requestBody)
        {

            return Client.Patch(
               url, meetingID, requestBody,
               authenticationToken);
        }
    }
    public interface IOnlineMeetingResponse
    {
        IOnlineMeeting[] value { get; }
    }

    public interface IOnlineMeeting
    {
        string id { get; }

        string subject { get; }

        DateTime startDateTime { get; }

        DateTime endDateTime { get; }
    }

    [Serializable]
    internal class GraphOnlineMeetingResponse : RestResponse, IOnlineMeetingResponse
    {
        [JsonProperty("value", ItemConverterType = typeof(ConcreteConverter<GraphOnlineMeeting>))]
        public IOnlineMeeting[] value { get; set; } = new GraphOnlineMeeting[0];
    }

    [Serializable]
    internal class GraphOnlineMeeting : IOnlineMeeting
    {
        public string id { get; set; }

        public string subject { get; set; }

        public DateTime startDateTime { get; set; }

        public DateTime endDateTime { get; set; }
    }
}
