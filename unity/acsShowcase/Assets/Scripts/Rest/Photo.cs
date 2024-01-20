// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System.Collections.Generic;
using System.Threading.Tasks;


namespace Azure.Communication.Calling.Unity.Rest
{
    public static class Photo 
    {
        /// <summary>
        /// Get the signed in user's profile picture Microsoft Graph
        /// </summary>
        public static Task<byte[]> Get(
            string authenticationToken)
        {
            QueryBuilder builder = new QueryBuilder("https://graph.microsoft.com/v1.0/me/photo/$value", maxArguments: 0);
            return Client.Get(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get the signed in user's profile picture Microsoft Graph
        /// </summary>
        public static Task<byte[]> Get(
            string authenticationToken,
            PhotoSize size)
        {
            if (size == PhotoSize.Any)
            {
                return Get(authenticationToken);
            }

            QueryBuilder builder = new QueryBuilder($"https://graph.microsoft.com/v1.0/me/photos/{size.ToUrl()}/$value", maxArguments: 0);
            return Client.Get(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get a user's profile picture Microsoft Graph
        /// </summary>
        public static Task<byte[]> Get(
            string authenticationToken,
            string userId)
        {
            Client.ValidateString(nameof(userId), userId);

            QueryBuilder builder = new QueryBuilder($"https://graph.microsoft.com/v1.0/users/{userId}/photo/$value", maxArguments: 0);
            return Client.Get(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get a user's profile picture Microsoft Graph
        /// </summary>
        public static Task<byte[]> Get(
            string authenticationToken,
            string userId,
            PhotoSize size)
        {
            if (size == PhotoSize.Any)
            {
                return Get(authenticationToken, userId);
            }

            Client.ValidateString(nameof(userId), userId);

            QueryBuilder builder = new QueryBuilder($"https://graph.microsoft.com/v1.0/users/{userId}/photos/{size.ToUrl()}/$value", maxArguments: 0);
            return Client.Get(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get all user's profile picture Microsoft Graph
        /// </summary>
        ///
        public static Task<string> Get(
            string authenticationToken,
            List<string> userIds,
            PhotoSize size)
        {
            int count = 1;
            string url = $"https://graph.microsoft.com/v1.0/$batch";
            string querry = "{\r\n\"requests\": [";
            foreach(var userId in userIds)
            {
                //Client.ValidateString(nameof(userId), userId);
                var querryPart = "{" + $"\r\n\"url\": \"/users/{userId}/photos/{size.ToUrl()}/$value\",\r\n\"method\": \"GET\",\r\n\"id\": \"{count}\"\r\n" + "}"; 
                if(count < userIds.Count)
                    querryPart += ",";
                querry += querryPart;
                count++;
            }
            querry += "]\r\n}"; 

            return Client.Post(
                url, querry, 
                authenticationToken);
        }
    }

    public enum PhotoSize
    {
        Any,

        Size_48x48,
        Size_64x64,
        Size_96x96,
        Size_120x120,
        Size_240x240, 
        Size_360x360, 
        Size_432x432, 
        Size_504x504,
        Size_648x648
    }

    public static class PhotoSizeExtensions
    {
        static Dictionary<PhotoSize, int[]> _sizeMap = new Dictionary<PhotoSize, int[]>()
        { 
            { PhotoSize.Size_48x48, new int[] { 48, 48 } },
            { PhotoSize.Size_64x64, new int[] { 64, 64 } },
            { PhotoSize.Size_96x96, new int[] { 96, 96 } },
            { PhotoSize.Size_120x120, new int[] { 120, 120 } },
            { PhotoSize.Size_240x240, new int[] { 240, 240 } },
            { PhotoSize.Size_360x360, new int[] { 360, 360 } },
            { PhotoSize.Size_432x432, new int[] { 432, 432 } },
            { PhotoSize.Size_504x504, new int[] { 504, 504 } },
            { PhotoSize.Size_648x648, new int[] { 648, 648 } }
        };

        public static string ToUrl(this PhotoSize photoSize)
        {
            var size = _sizeMap[photoSize];
            return $"{size[0]}x{size[1]}";
        }

        public static int GetWidth(this PhotoSize photoSize)
        {
            return _sizeMap[photoSize][0];
        }

        public static int GetHeight(this PhotoSize photoSize)
        {
            return _sizeMap[photoSize][1];
        }
    }

}