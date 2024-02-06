// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Azure.Communication.Calling.Unity.Rest
{
    internal class Client
    {
        private static HttpClient _client = null;

        // Set to true if using a proxy, like Fiddler.
        private static bool _useProxy = false;

        static Client()
        {
            // Avoid bugs by preventing proxy usages on deployed players.
            if (_useProxy && UnityEngine.Application.isEditor)
            {
                _client = new HttpClient(new HttpClientHandler()
                {
                    Proxy = new WebProxy()
                });
            }
            else
            {
                _client = new HttpClient();
            }
        }

        /// <summary>
        /// Vaidate endpoint value, and throw exception if invalid.
        /// </summary>
        internal static void ValidateEndpoint(string endpoint)
        {
            ValidateString(nameof(endpoint), endpoint);
        }

        /// <summary>
        /// Vaidate endpoint value, and throw exception if invalid.
        /// </summary>
        internal static void ValidateAccountKey(string accountKey)
        {
            ValidateString(nameof(accountKey), accountKey);
        }

        /// <summary>
        /// Vaidate rest string value, and throw exception if invalid.
        /// </summary>
        internal static void ValidateString(string name, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException($"The {name} parameter was null");
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"The {name} parameter was an empty string");
            }
        }


        /// <summary>
        /// Vaidate rest timespan value, and throw exception if invalid.
        /// </summary>
        internal static void ValidateTimeSpan(string name, TimeSpan value, TimeSpan minValue, TimeSpan maxValue)
        {
            if (value < minValue)
            {
                throw new ArgumentNullException($"The {name} parameter is smaller than the min value {minValue}");
            }
            else if (value > maxValue)
            {
                throw new ArgumentNullException($"The {name} parameter is larger than the max value {maxValue}");
            }
        }

        /// <summary>
        /// Vaidate rest string object, and throw exception if invalid.
        /// </summary>
        internal static void ValidateObject(string name, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException($"The {name} parameter is null");
            }
        }

        /// <summary>
        /// Validate rest integer is greater than zero.
        /// </summary>
        internal static void ValidateNonZeroInteger(string name, int value)
        {
            if (value == 0)
            {
                throw new ArgumentNullException($"The {name} parameter was zero");
            }
            else if (value < 0)
            {
                throw new ArgumentException($"The {name} parameter was negative");
            }
        }

        /// <summary>
        /// Perform an authenticated Http request.
        /// </summary>
        internal static Task<byte[]> Get(
            string url,
            AuthenticationType authenticationType = AuthenticationType.None,
            string authenticationSource = null)
        { 
            return RequestBytes<RestRequest, RestRequest>(
                HttpMethod.Get,
                url,
                authenticationType,
                authenticationSource,
                content: null);
        }

        public static async Task<string> Post(string endpoint, string jsonData, string accessToken)
        {
            var status = string.Empty;
            try
            { 
                string queryParameter = "";

                // pass body data  
                var body = new StringContent(jsonData, Encoding.UTF8, "application/json");


                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint + queryParameter))
                    {
                        request.Content = body;
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); 
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                        using (var response = await client.SendAsync(request))
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                                return await response.Content.ReadAsStringAsync();
                            else
                                status = $"Unable to access {endpoint}: {response.StatusCode}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                status = $"Error getting photos: {ex.Message}";
            }

            return null;
        }
        public static async Task<string> Patch(string endpoint, string joinWebUrl, string jsonData, string accessToken)
        {
            var status = string.Empty;
            try
            { 
                // pass body data  
                var body = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var method = new HttpMethod("PATCH");
                var request = new HttpRequestMessage(method, endpoint + joinWebUrl)
                {
                    Content = body
                };

                using (var client = new HttpClient())
                {
                    using (request)
                    {  
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                        using (var response = await client.SendAsync(request))
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                
                                var responseStr =  await response.Content.ReadAsStringAsync();
                                Debug.Log("Patch response: " + responseStr);
                                return responseStr;
                            }
                            else
                                status = $"Unable to access {endpoint}: {response.StatusCode}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                status = $"Error getting photos: {ex.Message}";
            }

            return null;
        }

        /// <summary>
        /// Perform an authenticated Http request.
        /// </summary>
        internal static async Task<byte[]> Post(
            string url, string content,
            AuthenticationType authenticationType = AuthenticationType.None,
            string authenticationSource = null)
        {
            //return RequestBytes<RestRequest, RestRequest>(
            //    HttpMethod.Post,
            //    url,
            //    authenticationType,
            //    authenticationSource,
            //    content);
             
            Log.Verbose<Client>("HTTP request begin ({0}:{1})", url, HttpMethod.Post);
            byte[] result = null;
            RestHelperWebException webException = null;

            string contentString = await Task.Run(() =>
            {
                if (content == null)
                {
                    return string.Empty;
                }
                else
                {
                    return JsonSerializer.Serialize(content);
                }
            });


            if (!string.IsNullOrEmpty(url))
            {
                MemoryStream memoryStream = null;
                HttpRequestMessage webRequestMessage = null;
                HttpResponseMessage webResponse = null;

                try
                {
                    webRequestMessage = new HttpRequestMessage(HttpMethod.Post, url); 

                    if (content != null)
                    {
                        webRequestMessage.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    }

                    // Add the request headers for authorizing request
                    Authentication.AddAuthorizationHeader(authenticationType, authenticationSource, webRequestMessage);

                    // Make request and get response message as a stream
                    webResponse = await _client.SendAsync(webRequestMessage);
                    if (webResponse.IsSuccessStatusCode)
                    {
                        Log.Verbose<Client>("HTTP request succeeded ({0}:{1}) -> {2}", url, HttpMethod.Post, webResponse.StatusCode);
                        result = await webResponse.Content.ReadAsByteArrayAsync();
                        var length = result == null ? 0 : result.Length;
                        Log.Verbose<Client>("HTTP request byte length ({0}:{1}) -> {2}", url, HttpMethod.Post, length);
                    }
                    else
                    {
                        Log.Error<Client>("HTTP request failed ({0}:{1}) -> {2}", url, HttpMethod.Post, webResponse.StatusCode);
                        webException = new RestHelperWebException(webRequestMessage.RequestUri, webResponse.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error<Client>("HTTP request failed ({0}:{1}) -> Exception\r\n{2}", url, HttpMethod.Post, ex);
                    throw ex;
                }
                finally
                {
                    memoryStream?.Close();
                    webResponse?.Dispose();
                    webRequestMessage?.Dispose();
                }
            }

            if (webException != null)
            {
                throw webException;
            }
            else if (result == null)
            {
                throw new Exception(string.Format("HTTP request failed. Failed to get response ({0}:{1})", url, HttpMethod.Post));
            }

            return result;
        }
        /// <summary>
        /// Perform an authenticated Http request.
        /// </summary>
        internal static Task<TResult> Get<TResult, TResultDeserialize>(
            string url,
            AuthenticationType authenticationType = AuthenticationType.None,
            string authenticationSource = null)
            where TResult : class
            where TResultDeserialize : RestResponse, TResult
        {
            return Request<RestRequest, RestRequest, TResult, TResultDeserialize>(
                HttpMethod.Get,
                url,
                authenticationType,
                authenticationSource,
                content: null);
        }

        /// <summary>
        /// Perform an authenticated Http request.
        /// </summary>
        internal static Task<TResult> Get<TResult>(
            string url,
            AuthenticationType authenticationType = AuthenticationType.None,
            string authenticationSource = null)
            where TResult : RestResponse
        {
            return Request<RestRequest, TResult>(
                HttpMethod.Get,
                url,
                authenticationType,
                authenticationSource,
                content: null);
        }

        /// <summary>
        /// Perform an authenticated Http request.
        /// </summary>
        internal static Task<TResult> Request<TContent, TResult>(
            HttpMethod method,
            string url,
            AuthenticationType authenticationType = AuthenticationType.None,
            string authenticationSource = null,
            TContent content = null)
            where TResult : RestResponse
            where TContent : RestRequest
        {
            return Request<TContent, TContent, TResult, TResult>(method, url, authenticationType, authenticationSource, content);
        }

        /// <summary>
        /// Perform an authenticated Http request.
        /// </summary>
        internal static async Task<TResult> Request<TContent, TContentSerialize, TResult, TResultDeserialize>(
            HttpMethod method,
            string url,
            AuthenticationType authenticationType = AuthenticationType.None,
            string authenticationSource = null,
            TContent content = null)
            where TResult : class
            where TResultDeserialize : RestResponse, TResult
            where TContent : class
            where TContentSerialize : RestRequest, TContent
        {
            Log.Verbose<Client>("HTTP request begin ({0}:{1})", url, method);
            TResult result = null;
            RestHelperWebException webException = null;
            bool expectingResponse = typeof(TResultDeserialize) != typeof(RestResponse);

            string contentString = await Task.Run(() =>
            {
                if (content == null)
                {
                    return string.Empty;
                }
                else
                {
                    return JsonSerializer.Serialize(content as TContentSerialize);
                }
            });


            if (!string.IsNullOrEmpty(url))
            {
                string response = null;
                MemoryStream memoryStream = null;
                HttpRequestMessage webRequestMessage = null;
                HttpResponseMessage webResponse = null;

                try
                {
                    webRequestMessage = new HttpRequestMessage(method, url);

                    if (content != null)
                    {
                        webRequestMessage.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    }

                    // Add the request headers for authorizing request
                    Authentication.AddAuthorizationHeader(authenticationType, authenticationSource, webRequestMessage);

                    // Make request and get response message as a stream
                    webResponse = await _client.SendAsync(webRequestMessage);
                    if (webResponse.IsSuccessStatusCode)
                    {
                        Log.Verbose<Client>("HTTP request succeeded ({0}:{1}) -> {2}", url, method, webResponse.StatusCode);
                        response = await webResponse.Content.ReadAsStringAsync();
                        var length = response == null ? 0 : response.Length;
                        Log.Verbose<Client>("HTTP request response length ({0}:{1}) -> {2}", url, method, length);
                    }
                    else
                    {
                        Log.Error<Client>("HTTP request failed ({0}:{1}) -> {2}", url, method, webResponse.StatusCode);
                        webException = new RestHelperWebException(webRequestMessage.RequestUri, webResponse.StatusCode);
                    }

                    // Deserialize XML
                    if (expectingResponse && !string.IsNullOrEmpty(response))
                    {
                        result = await Task.Run(() =>
                        {
                            return JsonSerializer.Deserialize<TResultDeserialize>(response);
                        });

                        if (result == null)
                        {
                            Log.Error<Client>("HTTP response parse failed ({0}:{1})", url, method);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error<Client>("HTTP request failed ({0}:{1}) -> Exception\r\n{2}", url, method, ex);
                    throw ex;
                }
                finally
                {
                    memoryStream?.Close();
                    webResponse?.Dispose();
                    webRequestMessage?.Dispose();
                }
            }

            if (webException != null)
            {
                throw webException;
            }
            else if (expectingResponse && result == null)
            {
                throw new Exception(string.Format("HTTP request failed. Failed to get response from content ({0}:{1})", url, method));
            }

            return result;
        }

        /// <summary>
        /// Perform an authenticated Http request, and return binary data
        /// </summary>
        internal static async Task<byte[]> RequestBytes<TContent, TContentSerialize>(
            HttpMethod method,
            string url,
            AuthenticationType authenticationType = AuthenticationType.None,
            string authenticationSource = null,
            TContent content = null)
            where TContent : class
            where TContentSerialize : RestRequest, TContent
        {
            Log.Verbose<Client>("HTTP request begin ({0}:{1})", url, method);
            byte[] result = null;
            RestHelperWebException webException = null;

            string contentString = await Task.Run(() =>
            {
                if (content == null)
                {
                    return string.Empty;
                }
                else
                {
                    return JsonSerializer.Serialize(content as TContentSerialize);
                }
            });


            if (!string.IsNullOrEmpty(url))
            {
                MemoryStream memoryStream = null;
                HttpRequestMessage webRequestMessage = null;
                HttpResponseMessage webResponse = null;

                try
                {
                    webRequestMessage = new HttpRequestMessage(method, url);

                    if (content != null)
                    {
                        webRequestMessage.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    }

                    // Add the request headers for authorizing request
                    Authentication.AddAuthorizationHeader(authenticationType, authenticationSource, webRequestMessage);

                    // Make request and get response message as a stream
                    webResponse = await _client.SendAsync(webRequestMessage);
                    if (webResponse.IsSuccessStatusCode)
                    {
                        Log.Verbose<Client>("HTTP request succeeded ({0}:{1}) -> {2}", url, method, webResponse.StatusCode);
                        result = await webResponse.Content.ReadAsByteArrayAsync();
                        var length = result == null ? 0 : result.Length;
                        Log.Verbose<Client>("HTTP request byte length ({0}:{1}) -> {2}", url, method, length);
                    }
                    else
                    {
                        Log.Error<Client>("HTTP request failed ({0}:{1}) -> {2}", url, method, webResponse.StatusCode);
                        webException = new RestHelperWebException(webRequestMessage.RequestUri, webResponse.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error<Client>("HTTP request failed ({0}:{1}) -> Exception\r\n{2}", url, method, ex);
                    throw ex;
                }
                finally
                {
                    memoryStream?.Close();
                    webResponse?.Dispose();
                    webRequestMessage?.Dispose();
                }
            }

            if (webException != null)
            {
                throw webException;
            }
            else if (result == null)
            {
                throw new Exception(string.Format("HTTP request failed. Failed to get response ({0}:{1})", url, method));
            }

            return result;
        }
        
        /// <summary>        
        /// Perform an authenticated Http request, and return binary data
        /// </summary>
        internal static async Task<Stream> RequestStream<TContent, TContentSerialize>(
            HttpMethod method,
            string url,
            AuthenticationType authenticationType = AuthenticationType.None,
            string authenticationSource = null,
            TContent content = null)
            where TContent : class
            where TContentSerialize : RestRequest, TContent
        {
            Log.Verbose<Client>("HTTP request begin ({0}:{1})", url, method);
            Stream result = null;
            RestHelperWebException webException = null;

            string contentString = await Task.Run(() =>
            {
                if (content == null)
                {
                    return string.Empty;
                }
                else
                {
                    return JsonSerializer.Serialize(content as TContentSerialize);
                }
            });


            if (!string.IsNullOrEmpty(url))
            {
                MemoryStream memoryStream = null;
                HttpRequestMessage webRequestMessage = null;
                HttpResponseMessage webResponse = null;

                try
                {
                    webRequestMessage = new HttpRequestMessage(method, url);

                    if (content != null)
                    {
                        webRequestMessage.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    }

                    // Add the request headers for authorizing request
                    Authentication.AddAuthorizationHeader(authenticationType, authenticationSource, webRequestMessage);

                    // Make request and get response message as a stream
                    webResponse = await _client.SendAsync(webRequestMessage);
                    if (webResponse.IsSuccessStatusCode)
                    {
                        Log.Verbose<Client>("HTTP request succeeded ({0}:{1}) -> {2}", url, method, webResponse.StatusCode);
                        result = await webResponse.Content.ReadAsStreamAsync();
                        var length = result == null ? 0 : result.Length;
                        Log.Verbose<Client>("HTTP request stream length ({0}:{1}) -> {2}", url, method, length);
                    }
                    else
                    {
                        Log.Error<Client>("HTTP request failed ({0}:{1}) -> {2}", url, method, webResponse.StatusCode);
                        webException = new RestHelperWebException(webRequestMessage.RequestUri, webResponse.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error<Client>("HTTP request failed ({0}:{1}) -> Exception\r\n{2}", url, method, ex);
                    throw ex;
                }
                finally
                {
                    memoryStream?.Close();
                    webResponse?.Dispose();
                    webRequestMessage?.Dispose();
                }
            }

            if (webException != null)
            {
                throw webException;
            }
            else if (result == null)
            {
                throw new Exception(string.Format("HTTP request failed. Failed to get response ({0}:{1})", url, method));
            }

            return result;
        }

        private static class JsonSerializer
        {
            public static string Serialize<T>(T value)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(value);
            }

            public static T Deserialize<T>(string value)
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
            }
        }
    }

    [Serializable]
    public class RestHelperWebException : Exception
    {
        public RestHelperWebException(Uri requestUri, HttpStatusCode httpStatusCode) : base($"Web request failed with {httpStatusCode} ({requestUri})")
        {
            RequestUri = requestUri;
            HttpStatusCode = httpStatusCode;
        }

        public Uri RequestUri { get; }

        public HttpStatusCode HttpStatusCode { get; }
    }

    public static class DateTimeExtensions
    {
        private static readonly string[] _formats = new string[] { "yyyy-MM-ddTHH:mm:ssK", "yyyy-MM-ddTHH:mm:ss.ffK", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.ffZ" };

        public static string ToRfc3339String(this DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToString("yyyy-MM-dd'T'HH:mm:sszzz", DateTimeFormatInfo.InvariantInfo);
            }
            else
            {
                return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ssZ", DateTimeFormatInfo.InvariantInfo);
            }
        }

        public static string ToRfc3339String(this DateTimeOffset dateTime)
        {
            return dateTime.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ssZ", DateTimeFormatInfo.InvariantInfo);
        }

        public static DateTimeOffset FromRtc3339String(this string dateTimeString)
        {
            if (!DateTimeOffset.TryParseExact(dateTimeString, _formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset result))
            {
                throw new ArgumentException("String is not a valid RFC3339 time", "dateTimeString");
            }
            return result;
        }
    }


    [Serializable]
    public class RestRequest
    {
    }

    [Serializable]
    public class RestResponse
    {
    }

    class WebProxy : IWebProxy
    {
        private IWebProxy wrappedProxy;
        private ICredentials creds;

        public WebProxy()
        {
            Initialize();
        }

        public WebProxy(System.Net.IWebProxy theWrappedProxy)
        {
            Initialize();
            wrappedProxy = theWrappedProxy;
        }

        public System.Net.ICredentials Credentials
        {
            get
            {
                if (wrappedProxy != null)
                {
                    return wrappedProxy.Credentials;
                }
                else
                {
                    return creds;
                }
            }
            set
            {
                if (wrappedProxy != null)
                {
                    wrappedProxy.Credentials = value;
                }
                else
                {
                    creds = value;
                }
            }
        }

        public Uri GetProxy(Uri destination)
        {
            if (wrappedProxy != null /* todo or Uri == certain Uri */)
            {
                return wrappedProxy.GetProxy(destination);
            }
            else
            {
                // hardcoded proxy here.. this is the Fiddler proxy
                return new Uri("http://127.0.0.1:8888");
            }
        }

        public bool IsBypassed(Uri host)
        {
            if (wrappedProxy != null)
            {
                return wrappedProxy.IsBypassed(host);
            }
            else
            {
                return false;
            }
        }

        private void Initialize()
        {
            wrappedProxy = null;
            creds = CredentialCache.DefaultCredentials;
        }
    }
}
