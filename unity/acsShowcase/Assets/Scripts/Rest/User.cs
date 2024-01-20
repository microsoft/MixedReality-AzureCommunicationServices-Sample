// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System.Threading.Tasks;

namespace Azure.Communication.Calling.Unity.Rest
{
    public class User
    {
        /// <summary>
        /// Get the signed in user's profile from Microsoft Graph
        /// </summary>
        public static Task<IUser> Get(
            string authenticationToken)
        {
            QueryBuilder builder = new QueryBuilder("https://graph.microsoft.com/v1.0/me", maxArguments: 0);
            return Client.Get<IUser, GraphUser>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        /// <summary>
        /// Get a user's profile from Microsoft Graph
        /// </summary>
        public static Task<IUser> Get(
            string authenticationToken,
            string userId)
        {
            Client.ValidateString(nameof(userId), userId);

            QueryBuilder builder = new QueryBuilder($"https://graph.microsoft.com/v1.0/users/{userId}", maxArguments: 0);
            return Client.Get<IUser, GraphUser>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }
    }
    public interface IUsers
    {
        public IUser[] value { get; }
    }

    public interface IUser
    {
        string displayName { get; }

        string jobTitle { get; }

        string givenName { get; }

        string surname { get; }

        string mail { get; }

        string userPrincipalName { get; }

        string officeLocation { get; }

        string id { get; }

        // 
        // Not part of service repsonse. Internal use only
        //
        InternalUserType internalType { get; }
    }

    public enum InternalUserType
    {
        /// <summary>
        /// The user type is not known.
        /// </summary>
        Unknown,

        /// <summary>
        /// This is a Teams (or Graph User)
        /// </summary>
        Teams,

        /// <summary>
        /// This is a "bring your own identity" user created by the Azure Communication Services.
        /// </summary>
        CommunicationServices,
    }

     

    public class GraphUser : RestResponse, IUser
    {
        public string displayName { get; set; } = string.Empty;

        public string jobTitle { get; set; } = string.Empty;

        public string mail { get; set; } = string.Empty;

        public string officeLocation { get; set; } = string.Empty;

        public string id { get; set; } = string.Empty;

        public string givenName { get; set; } = string.Empty;

        public string surname { get; set; } = string.Empty;

        public string userPrincipalName { get; set; } = string.Empty;

        // 
        // Not part of service repsonse. Internal use only
        //
        [Newtonsoft.Json.JsonIgnore]
        public InternalUserType internalType => InternalUserType.Teams;
}
}
