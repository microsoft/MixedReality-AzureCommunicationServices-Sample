// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Azure.Communication.Calling.Unity.Rest
{
    public static class People
    {
        /// <summary>
        /// Get the top relative people for the signed in user.
        /// </summary>
        public static Task<IUsers> Get(
            string authenticationToken,
            int count = -1,
            int skip = -1)
        {
            QueryBuilder builder = new QueryBuilder("https://graph.microsoft.com/v1.0/me/people", maxArguments: 2);
            if (count > 0)
            {
                builder.InsertArgument("$top", count.ToString());
            }

            if (skip > 0)
            {
                builder.InsertArgument("$skip", skip.ToString());
            }

            return Client.Get<IUsers, GraphPeople>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }
        
        
        /// <summary>
        /// Get people with their email addresses 
        /// </summary>
        public static Task<IUsers> Get(
            string authenticationToken,
            List<string> emailList,
            int count = -1,
            int skip = -1)
        {
            string allEmail = "(";
            for(int i=0; i<emailList.Count; i++)
            {
                if (i == 0)
                    allEmail += "'" + emailList[i] + "'";
                else
                    allEmail += ",'" + emailList[i] + "'";
            }

            allEmail += ")";
            string querryStr = "https://graph.microsoft.com/v1.0/users?$filter=mail in " + allEmail;
            
            QueryBuilder builder = new QueryBuilder(querryStr, maxArguments: 2);
            if (count > 0)
            {
                builder.InsertArgument("$top", count.ToString());
            }

            if (skip > 0)
            {
                builder.InsertArgument("$skip", skip.ToString());
            }

            return Client.Get<IUsers, GraphPeople>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }
        

        /// <summary>
        /// Search for people starting with the given prefix.
        /// </summary>
        public static Task<IUsers> Search(
            string authenticationToken,
            string query,
            int count = -1,
            int skip = -1)
        {
            if (string.IsNullOrEmpty(query))
            {
                return Task.FromResult<IUsers>(new GraphPeople());
            }

            QueryBuilder builder = new QueryBuilder("https://graph.microsoft.com/v1.0/me/people", maxArguments: 3);
            builder.InsertArgument("$search", query);

            if (count > 0)
            {
                builder.InsertArgument("$top", count.ToString());
            }

            if (skip > 0)
            {
                builder.InsertArgument("$skip", skip.ToString());
            }

            return Client.Get<IUsers, GraphPeople>(
                builder.ToString(),
                AuthenticationType.Token,
                authenticationToken);
        }

        //Search via Search endpoint
        public static Task<string> Search2(
                string authenticationToken,
                string query )
        {
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }
            var postQuery = "{\r\n\"requests\": [\r\n{\r\n\"entityTypes\": [\r\n\"person\"\r\n],\r\n\"query\": {\r\n\"queryString\": \"" + query  +"\"\r\n},\r\n\"from\": 0,\r\n\"size\": 50\r\n}\r\n]\r\n}";
              
            return Client.Post(
                $"https://graph.microsoft.com/v1.0/search/query",
                postQuery,
                authenticationToken);
        }
    }
    

[Serializable]
    public class GraphPeople : RestResponse, IUsers
    {
        [JsonProperty("value", ItemConverterType = typeof(ConcreteConverter<GraphPerson>))]
        public IUser[] value { get; set; } = new GraphPerson[0];
    }

    [Serializable]
    public class GraphPerson : IUser
    {
        public string id { get; set; } = string.Empty;

        public string displayName { get; set; } = string.Empty;

        public string givenName { get; set; } = string.Empty;

        public string surname { get; set; } = string.Empty;

        public string userPrincipalName { get; set; } = string.Empty;

        public string jobTitle { get; set; } = string.Empty;

        public string mail { get; set; } = string.Empty;

        public string officeLocation { get; set; } = string.Empty;

        //
        // These fields do not originate from the Graph APIs
        //

        [Newtonsoft.Json.JsonIgnore]
        public InternalUserType internalType { get; set; } = InternalUserType.Teams;
    }
}
