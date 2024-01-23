// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Azure.Communication.Calling.Unity
{
    internal static class HttpUtility
    {
        public static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var queryParams = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(queryString))
            {
                return queryParams;
            }

            if (queryString.StartsWith("?"))
            {
                queryString = queryString.Substring(1);
            }

            string[] paramChunks = queryString.Split('&');
            foreach (var queryParam in paramChunks)
            {
                string[] keyValuePair = queryParam.Split('=');
                queryParams.Add(keyValuePair[0], Uri.UnescapeDataString(keyValuePair[1]));
            }

            return queryParams;
        }

        public static string FindFreeLocalhostRedirectUri()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                return $"http://localhost:{port}";
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
