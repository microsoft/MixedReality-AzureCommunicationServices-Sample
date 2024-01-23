// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace azure_communications_helper_func
{
    internal static class AccessTokenRequestHelpers
    {
        public static async Task<AccessTokenRequest> Parse(HttpRequest request, ILogger log)
        {
            string requestBody;
            try
            {
                requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            }
            catch (Exception ex)
            {
                requestBody = null;    
                log.LogInformation($"Reading request content failed. Exception: {ex}");
            }

            AccessTokenRequest tokenRequest = null;
            if (requestBody != null)
            {
                try
                {
                    var jsonSerializerSettings = new JsonSerializerSettings();
                    jsonSerializerSettings.Converters.Add(new StringEnumConverter() { AllowIntegerValues = false });
                    tokenRequest = JsonConvert.DeserializeObject<AccessTokenRequest>(requestBody);
                }
                catch (Exception ex)
                {
                    tokenRequest = null;
                    log.LogInformation($"Parsing request json failed.\r\nJson:\r\n{requestBody}\r\nException:\r\n{ex}");
                }
            }
    
            return tokenRequest;
        }
    }
}
