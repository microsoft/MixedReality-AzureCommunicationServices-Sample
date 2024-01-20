// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Threading.Tasks;

namespace Azure.Communication.Calling.Unity.Rest
{
    internal class ChatInfo
    {

        /// <summary>
        /// Get the chat details of an online meeting from the Microsoft Graph.
        /// </summary>
        internal static async Task<IChatInfo> GetFromTeamsUrl(
            string authenticationToken,
            string joinWebUrl)
        {

            // TODO REMOVE TESTING STUFF
            var myProfile = await User.Get(authenticationToken);
            var myProfileUsingOther = await User.Get(authenticationToken, myProfile.id);

            var onlineMeetings = await OnlineMeeting.Get(authenticationToken, joinWebUrl);
            if (onlineMeetings.value != null && onlineMeetings.value.Length == 0)
            {
                return null;
            }
            else
            {
                return onlineMeetings.value[0]?.chatInfo;
            }
        }
    }

    public interface IChatInfo
    {
        string messageId { get; }
        string replyChainMessageId { get; }
        string threadId { get; }
    }

    [Serializable]
    internal class GraphChatInfo : IChatInfo
    {
        public string messageId { get; set; }

        public string replyChainMessageId { get; set; }

        public string threadId { get; set; }
    }
}
