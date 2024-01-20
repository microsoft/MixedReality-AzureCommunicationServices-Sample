// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Azure.Communication.Calling.Unity.Rest
{
    internal static class Authentication
    {
        /// <summary>
        /// This creates the authorization header. This is required, and must be built 
        ///   exactly following the instructions. This will return the authorization header
        ///   for most storage service calls.
        /// Create a string of the message signature and then encrypt it.
        /// </summary>
        internal static void AddAuthorizationHeader(
           AuthenticationType authenticationType,
           string authenticationSource,
           HttpRequestMessage httpRequestMessage)
        {
            if (!string.IsNullOrWhiteSpace(authenticationSource))
            {
                switch (authenticationType)
                {
                    case AuthenticationType.Key:
                        AddAuthorizationHeaderFromAccountKey(authenticationSource, httpRequestMessage);
                        break;

                    case AuthenticationType.Token:
                        AddAuthorizationHeaderFromToken(authenticationSource, httpRequestMessage);
                        break;
                }
            }
        }

        /// <summary>
        /// This creates the authorization header. This is required, and must be built 
        ///   exactly following the instructions. This will return the authorization header
        ///   for most storage service calls.
        /// Create a string of the message signature and then encrypt it.
        /// </summary>
        private static void AddAuthorizationHeaderFromAccountKey(
           string accountKey,
           HttpRequestMessage httpRequestMessage)
        {
            if (httpRequestMessage == null)
            {
                throw new ArgumentNullException(nameof(httpRequestMessage));
            }

            if (httpRequestMessage.RequestUri == null)
            {
                throw new ArgumentNullException(nameof(httpRequestMessage.RequestUri));
            }

            // Get the time
            DateTime now = DateTime.UtcNow;

            // This is the raw representation of the message signature.
            HttpMethod method = httpRequestMessage.Method;

            // Create message signature using this format:
            //
            // StringToSign = 
            //   VERB + "\n"
            //   URLPathAndQuery + "\n"
            //   DateHeaderValue + ";" + HostHeaderValue + ";" + ContentHashHeaderValue
            //

            string verb = method.ToString().ToUpper();
            string host = httpRequestMessage.RequestUri.Host;
            string contentHash = GetSha256FromContent(httpRequestMessage);
            string date = ToString(now);
            string urlPathAndQuery = httpRequestMessage.RequestUri.PathAndQuery;

            // Create a string that will be signed
            string signatureString =
                $"{verb}\n{urlPathAndQuery}\n{date};{host};{contentHash}";

            // Now turn it into a byte array.
            byte[] signatureBytes = Encoding.UTF8.GetBytes(signatureString);

            // Create the HMACSHA256 version of the storage key.
            string signature = null;
            using (HMACSHA256 sha256 = new HMACSHA256(Convert.FromBase64String(accountKey)))
            {
                // Compute the hash of the SignatureBytes and convert it to a base64 string.
                signature = Convert.ToBase64String(sha256.ComputeHash(signatureBytes));
            }

            // This is the header that will be added to the list of request headers.
            // You can stop the code here and look at the value before it is returned.
            AuthenticationHeaderValue authHV = new AuthenticationHeaderValue("HMAC-SHA256", $"SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature={signature}");

            // Finialize headers
            //httpRequestMessage.Headers.Add("X-FORWARDED-HOST", host);
            httpRequestMessage.Headers.Add("x-ms-content-sha256", contentHash);
            httpRequestMessage.Headers.Add("x-ms-date", date);
            httpRequestMessage.Headers.Authorization = authHV;
        }

        /// <summary>
        /// This creates the authorization header from a token
        /// </summary>
        private static void AddAuthorizationHeaderFromToken(
            string token,
           HttpRequestMessage httpRequestMessage)
        {
            //    authorizationHeaderValue = convertToBase64String(< security principal ID > +":" + < secret of the security principal >)
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private static string GetSha256FromContent(
           HttpRequestMessage httpRequestMessage)
        {
            var alg = SHA256.Create();

            using (var memoryStream = new MemoryStream())
            using (var contentHashStream = new CryptoStream(memoryStream, alg, CryptoStreamMode.Write))
            {
                httpRequestMessage.Content.CopyToAsync(contentHashStream).Wait();
            }

            return Convert.ToBase64String(alg.Hash!);
        }

        /// <summary>
        /// Convert a date time to a header string.
        /// </summary>
        private static string ToString(DateTime dateTime)
        {
            return dateTime.ToString("R", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// The type of rest header authentication to use
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// No authentication header is added.
        /// </summary>
        None,

        /// <summary>
        /// The authentication header will be generated from the Azure Communications Account Key
        /// </summary>
        Key,

        /// <summary>
        /// The authentication will be the specified token.
        /// </summary>
        Token
    }
}
