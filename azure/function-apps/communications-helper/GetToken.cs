// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Communication;
using Azure.Communication.Identity;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

using AccessToken = Azure.Core.AccessToken;

namespace azure_communications_helper_func
{
    public static class GetToken
    {
        [FunctionName("GetToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] 
            HttpRequest request,
            ILogger log,            
            ExecutionContext context)
        {       
            var tokenRequest = await AccessTokenRequestHelpers.Parse(request, log);
            if (tokenRequest == null || string.IsNullOrWhiteSpace(tokenRequest.id))
            {
                return new Microsoft.AspNetCore.Mvc.BadRequestResult();
            }            

            var configuration = Helpers.LoadConfiguration(context, log);
            string communicationsEndpointString = configuration[Constants.CommunicationsEndpointKey];
            
            AccessToken accessToken;
            var communicationsEndpoint = new Uri(communicationsEndpointString);
            var communicationsCredential = new ManagedIdentityCredential();
            var communicationUserIdentifier = new CommunicationUserIdentifier(tokenRequest.id);
            CommunicationIdentityClient client = new CommunicationIdentityClient(communicationsEndpoint, communicationsCredential);
            try
            {
                accessToken = client.GetToken(communicationUserIdentifier, tokenRequest?.scopes ?? new CommunicationTokenScope[0]);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Creating token failed. Exception: {ex}");
                return new Microsoft.AspNetCore.Mvc.NotFoundResult();
            }

            string responseMessage = Helpers.SerializeWithCamelCasing(new AccessTokenResponse() 
            {
                token = accessToken.Token,
                expiresOn = accessToken.ExpiresOn
            });
            
            return new OkObjectResult(responseMessage);
        }
    }
}
