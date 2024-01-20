// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Threading.Tasks;

namespace Azure.Communication.Calling.Unity.Rest
{
    internal static class Chat
    {
        /// <summary>
        /// Get all chat messages for a chat thread
        /// </summary>
        internal static Task<IMessages> GetMessages(
            MeetingServiceIdentity identity,
            string chatThreadId,
            int maxPageSize = -1,
            DateTime? startTime = null)
        {
            if (identity == null || identity.LocalParticipant == null || !identity.LocalParticipant.IsLocal)
            {
                throw new ArgumentException("Given identity is invalid");
            }

            if (identity.LocalParticipant.IsGuest)
            {
                return GetMessagesAsGuest(identity.Endpoint, identity.CommunicationAccessToken, chatThreadId, maxPageSize, startTime);
            }
            else
            {
                return GetMessages(identity.GraphAccessToken, chatThreadId, maxPageSize, startTime);
            }
        }

        /// <summary>
        /// Get all chat messages for a chat thread 
        /// </summary>
        internal static Task<IMessages> GetMessages(
            MeetingServiceIdentity identity,
            IMessages previous)
        {
            if (identity == null || identity.LocalParticipant == null || !identity.LocalParticipant.IsLocal)
            {
                throw new ArgumentException("Given identity is invalid");
            }

            if (identity.LocalParticipant.IsGuest)
            {
                return GetMessagesAsGuest(identity.CommunicationAccessToken, previous);
            }
            else
            {
                return GetMessages(identity.GraphAccessToken, previous);
            }
        }

        /// <summary>
        /// Get all chat messages for a chat thread using the Azure Communication Services.
        /// This request only works for guest users.
        /// </summary>
        internal static Task<IMessages> GetMessagesAsGuest(
            string endpoint,
            string authenticationToken,
            string chatThreadId,
            int maxPageSize = -1,
            DateTime? startTime = null)
        {
            Client.ValidateEndpoint(endpoint);
            Client.ValidateString(nameof(chatThreadId), chatThreadId);

            QueryBuilder builder = new QueryBuilder($"{endpoint}/chat/threads/{chatThreadId}/messages", maxArguments: 3);
            if (maxPageSize > 0)
            {
                builder.InsertArgument("maxPageSize", maxPageSize.ToString());
            }

            if (startTime != null)
            {
                builder.InsertArgument("startTime", startTime.Value.ToRfc3339String());
            }

            builder.InsertArgument("api-version", "2021-09-07");

            return Client.Get<IMessages, CommunicationMessagesResponse>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get all chat messages for a chat thread using the Azure Communication Services.
        /// </summary>
        internal static Task<IMessages> GetMessagesAsGuest(
            string authenticationToken,
            IMessages previous)
        {
            return Client.Get<IMessages, CommunicationMessagesResponse>(
                previous.nextLink,
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get all chat messages for a chat thread using Microsoft Graph
        /// This request only works for Teams users.
        /// </summary>
        internal static Task<IMessages> GetMessages(
            string authenticationToken,
            string chatThreadId,
            int maxPageSize = -1,
            DateTime? startTime = null)
        {
            Client.ValidateString(nameof(chatThreadId), chatThreadId);

            QueryBuilder builder = new QueryBuilder($"https://graph.microsoft.com/v1.0/me/chats/{chatThreadId}/messages", maxArguments: 1);
            if (maxPageSize > 0)
            {
                builder.InsertArgument("$top", maxPageSize.ToString());
            }

            return Client.Get<IMessages, GraphMessagesResponse>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get all chat messages for a chat thread using Microsoft Graph
        /// This request only works for Teams users.
        /// </summary>
        internal static Task<IMessages> GetMessages(
            string authenticationToken,
            IMessages previous)
        {
            return Client.Get<IMessages, GraphMessagesResponse>(
                previous.nextLink,
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Send a chat message to a chat thread
        /// </summary>
        internal static Task<ISendMessageResponse> SendMessage(
            MeetingServiceIdentity identity,
            string chatThreadId,
            string message)
        {
            if (identity == null || identity.LocalParticipant == null || !identity.LocalParticipant.IsLocal)
            {
                throw new ArgumentException("Given identity is invalid");
            }

            if (identity.LocalParticipant.IsGuest)
            {
                return SendMessageAsGuest(identity.Endpoint, identity.CommunicationAccessToken, identity.LocalParticipant.DisplayName ?? "Unknown", chatThreadId, message);
            }
            else
            {
                return SendMessage(identity.GraphAccessToken, chatThreadId, message);
            }
        }

        /// <summary>
        /// As a signed in Teams user, send a chat message to a chat thread using the Microsoft Graph 
        /// </summary>
        internal static Task<ISendMessageResponse> SendMessage(
            string authenticationToken,
            string chatThreadId,
            string message)
        {
            Client.ValidateString(nameof(chatThreadId), chatThreadId);
            Client.ValidateString(nameof(message), message);

            GraphSendMessageRequest request = new GraphSendMessageRequest()
            {
                body = new GraphSendMessageBody()
                {
                    content = message,
                    contentType = GraphMessageContentType.text
                }
            };

            return Client.Request<GraphSendMessageRequest, GraphSendMessageRequest, ISendMessageResponse, SendMessageResponse>(
                System.Net.Http.HttpMethod.Post,
                $"https://graph.microsoft.com/v1.0/chats/{chatThreadId}/messages",
                AuthenticationType.Token,
                authenticationToken,
                request);
        }

        /// <summary>
        /// As a guest, send a chat message to a chat thread using the Azure Communication Services
        /// </summary>
        internal static Task<ISendMessageResponse> SendMessageAsGuest(
            string endpoint,
            string authenticationToken,
            string sender,
            string chatThreadId,
            string message)
        {
            Client.ValidateEndpoint(endpoint);
            Client.ValidateString(nameof(chatThreadId), chatThreadId);
            Client.ValidateString(nameof(sender), sender);
            Client.ValidateString(nameof(message), message);

            CommunicationSendMessageRequest request = new CommunicationSendMessageRequest()
            {
                content = message,
                senderDisplayerName = sender,
                typeEnum = MessageContentType.text
            };

            return Client.Request<CommunicationSendMessageRequest, CommunicationSendMessageRequest, ISendMessageResponse, SendMessageResponse>(
                System.Net.Http.HttpMethod.Post,
                $"{endpoint}/chat/threads/{chatThreadId}/message?api-version=2021-09-07",
                AuthenticationType.Token,
                authenticationToken,
                request);
        }
    }

    internal class CommunicationsMessageTypeToEnum : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string) || objectType == typeof(MessageContentType);
        }
        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            MessageContentType type = MessageContentType.unknown;
            if (reader.TokenType == JsonToken.String)
            {
                string value = (string)reader.Value;
                Enum.TryParse(value, out type);
            }
            return type;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    internal class GraphMessageTypeToEnum : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string) || objectType == typeof(MessageContentType);
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            MessageContentType type = MessageContentType.unknown;
            if (reader.TokenType == JsonToken.String)
            {
                string value = (string)reader.Value;
                if (Enum.TryParse(value, out GraphMessageType graphType))
                {
                    switch (graphType)
                    {
                        case GraphMessageType.message:
                            type = MessageContentType.unknown;
                            break;
                        case GraphMessageType.chatEvent:
                            type = MessageContentType.unknown;
                            break;
                        case GraphMessageType.typing:
                            type = MessageContentType.unknown;
                            break;
                        case GraphMessageType.unknownFutureValue:
                            type = MessageContentType.systemEventMessage;
                            break;
                        case GraphMessageType.systemEventMessage:
                            type = MessageContentType.systemEventMessage;
                            break;
                    }
                }
            }
            return type;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    public interface IMessages
    {
        IMessage[] value { get; }

        string nextLink { get; }
    }

    public interface IMessage
    {
        string id { get; }

        DateTime createdDateTime { get; }

        IMessageContent content { get; }

        MessageContentType contentType { get; }

        IMessageSender sender { get; }

        string senderDisplayName { get; }
    }

    public interface IMessageContent
    {
         string message { get; }
    }

    public interface IMessageSender
    {
        IMessageUser user { get; }
    }

    public interface IMessageUser
    {
        string id { get; }
    }

    public interface ISendMessageResponse
    {
        string id { get; }
    }

    [Serializable]
    internal class CommunicationMessagesResponse : RestResponse, IMessages
    {
        [JsonProperty("value", ItemConverterType = typeof(ConcreteConverter<CommunicationMessage>))]
        public IMessage[] value { get; set; } = new CommunicationMessage[0];

        public string nextLink { get; set; } = string.Empty;
    }

    [Serializable]
    internal class GraphMessagesResponse : RestResponse, IMessages
    {
        [JsonProperty("value", ItemConverterType = typeof(ConcreteConverter<GraphMessage>))]
        public IMessage[] value { get; set; } = new GraphMessage[0];

        [Newtonsoft.Json.JsonProperty("@odata.nextLink")]
        public string nextLink { get; set; } = string.Empty;
    }

    [Serializable]
    internal class CommunicationMessage : RestResponse, IMessage
    {
        public string id { get; set; } = string.Empty;
        
        [JsonProperty("createdOn")]
        public DateTime createdDateTime { get; set; } = DateTime.MinValue;

        [JsonConverter(typeof(ConcreteConverter<CommunicationMessageContent>))]
        public IMessageContent content { get; set; } = new CommunicationMessageContent();

        [JsonProperty("type")]
        [JsonConverter(typeof(CommunicationsMessageTypeToEnum))]
        public MessageContentType contentType { get; set; } = MessageContentType.unknown;

        [JsonProperty("senderCommunicationIdentifier")]
        [JsonConverter(typeof(ConcreteConverter<CommunicationMessageSender>))]
        public IMessageSender sender { get; set; } = new CommunicationMessageSender();

        public string senderDisplayName { get; set; } = string.Empty;
    }

    [Serializable]
    internal class GraphMessage : RestResponse, IMessage
    {
        private MessageContentType _type = MessageContentType.unknown;

        public string id { get; set; } = string.Empty;

        public DateTime createdDateTime { get; set; } = DateTime.MinValue;

        [JsonProperty("body")]
        [JsonConverter(typeof(ConcreteConverter<GraphMessageContent>))]
        public IMessageContent content { get; set; } = new GraphMessageContent();

        [JsonProperty("messageType")]
        [JsonConverter(typeof(GraphMessageTypeToEnum))]
        public MessageContentType contentType
        {
            get
            {
                if (_type == MessageContentType.unknown)
                {
                    var graphContent = content as GraphMessageContent;
                    GraphMessageContentType innerContentType = graphContent?.contentType ?? GraphMessageContentType.unknown;
                    if (innerContentType == GraphMessageContentType.text)
                    {
                        _type = MessageContentType.text;
                    }
                    else if (innerContentType == GraphMessageContentType.html)
                    {
                        _type = MessageContentType.html;
                    }
                }

                return _type;
            }

            set { _type = value; }
        }

        [JsonProperty("from")]
        [JsonConverter(typeof(ConcreteConverter<GraphMessageSender>))]
        public IMessageSender sender { get; set; } = new GraphMessageSender();

        [JsonIgnore]
        public string senderDisplayName
        {
            get
            {
                var user = sender?.user as GraphMessageUser;
                return user?.displayName;
            }
        }
    }

    [Serializable]
    internal class CommunicationMessageContent : IMessageContent
    {
        public string message { get; set; } = string.Empty;
    }

    [Serializable]
    internal class GraphMessageContent : IMessageContent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public GraphMessageContentType contentType { get; set; } = GraphMessageContentType.unknown;

        [JsonProperty("content")]
        public string message { get; set; } = string.Empty;
    }

    [Serializable]
    internal class CommunicationMessageSender : IMessageSender
    {
        [JsonProperty("communiciationUser")]
        [JsonConverter(typeof(ConcreteConverter<CommuniciationUser>))]
        public IMessageUser user { get; set; } = new CommuniciationUser();
    }

    [Serializable]
    internal class GraphMessageSender : IMessageSender
    {
        [JsonConverter(typeof(ConcreteConverter<GraphMessageUser>))]
        public IMessageUser user { get; set; } = new GraphMessageUser();
    }

    [Serializable]
    internal class CommuniciationUser : IMessageUser
    {
        public string id { get; set; } = string.Empty;
    }

    [Serializable]
    internal class GraphMessageUser : IMessageUser
    {
        public string id { get; set; } = string.Empty;
        public string displayName { get; set; } = string.Empty;
    }

    [Serializable]
    internal class GraphSendMessageRequest : RestRequest
    {
        public string subject = string.Empty;

        public GraphSendMessageBody body { get; set; } = new GraphSendMessageBody();
    }

    [Serializable]
    internal class GraphSendMessageBody
    {
        public string content { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public GraphMessageContentType contentType { get; set; }
    }

    [Serializable]
    internal class CommunicationSendMessageRequest : RestRequest
    {
        public string content { get; set; } = string.Empty;
        //public MessageMetadata metadata { get; set; } = new MessageMetadata();
        public string senderDisplayerName { get; set; } = string.Empty;
        public string type { get; set; } = MessageContentType.unknown.ToString();

        [Newtonsoft.Json.JsonIgnore]
        public MessageContentType typeEnum
        {
            get
            {
                Enum.TryParse<MessageContentType>(type, out MessageContentType value);
                return value;
            }

            set
            {
                type = value.ToString();
            }
        }
    }

    [Serializable]
    public class SendMessageResponse : RestResponse, ISendMessageResponse
    {
        public string id { get; set; } = string.Empty;
    }

    public enum MessageContentType
    {
        /// <summary>
        /// This not valid service value.
        /// </summary>
        unknown,

        /// <summary>
        /// This not a value from the service, but is used to indication a system message.
        /// </summary>
        systemEventMessage,

        html,
        participantAdded,
        participantRemoved,
        text,
        topicUpdated
    }

    internal enum GraphMessageType
    {
        /// <summary>
        /// This not valid service value.
        /// </summary>
        unknown,

        message,
        chatEvent,
        typing,
        unknownFutureValue,
        systemEventMessage
    }

    internal enum GraphMessageContentType
    {
        /// <summary>
        /// This not valid service value.
        /// </summary>
        unknown,

        text,
        html
    }
}
