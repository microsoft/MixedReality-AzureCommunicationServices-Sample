// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using UnityEngine;

namespace Azure.Communication.Calling.Unity
{
    internal class DefaultOSBrowserWebUI : ICustomWebUi
    {
        private readonly SystemWebViewOptions _systemWebViewOptions;

        private readonly SynchronizationContext _synchronizationContext;

        public DefaultOSBrowserWebUI(SystemWebViewOptions systemWebViewOptions, SynchronizationContext synchronizationContext)
        {
            _systemWebViewOptions = systemWebViewOptions;
            _synchronizationContext = synchronizationContext;
        }

        public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
        {
            // Open browser window on what should be Unity's main thread
            _synchronizationContext.Send((state) => Application.OpenURL(authorizationUri.ToString()), null);
            return await InterceptAuthorizationUriAsync(redirectUri);
        }

        private async Task<Uri> InterceptAuthorizationUriAsync(Uri redirectUri)
        {
            HttpListener httpListener = new HttpListener();
            string urlPrefix = redirectUri.OriginalString;
            urlPrefix += !urlPrefix.EndsWith("/") ? "/" : string.Empty;
            httpListener.Prefixes.Add(urlPrefix);
            httpListener.Start();

            try
            {
                HttpListenerContext context = await httpListener.GetContextAsync();

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(GetResponseMessage(context.Request.Url));
                context.Response.ContentLength64 = buffer.Length;

                using (Stream output = context.Response.OutputStream)
                {
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }

                return context.Request.Url;
            }
            finally
            {
                httpListener.Stop();
            }
        }

        private string GetResponseMessage(Uri authCodeUri)
        {
            var queryParams = HttpUtility.ParseQueryString(authCodeUri.Query);
            if (queryParams.ContainsKey("code"))
            {
                return _systemWebViewOptions.HtmlMessageSuccess;
            }

            return string.Format(
                _systemWebViewOptions.HtmlMessageError,
                queryParams["error"],
                queryParams["error_description"]
            );
        }
    }
}
#endif