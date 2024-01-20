using Azure.Communication.Identity;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

using AccessToken = Azure.Core.AccessToken;

namespace azure_communications_helper_func
{
    public static class GetTokenForTeamsUser
    {        
        [FunctionName("GetTokenForTeamsUser")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] 
            HttpRequest request,
            ILogger log, 
            ExecutionContext context)
        {
            ExchangeAccessTokenRequest tokenRequest = await ExtractTokenFromContent(request, log);
            if (tokenRequest == null)
            {
                log.LogInformation($"Exchanging ACS token from Teams token failed. Teams token was empty.");
                return new BadRequestResult();
            }            

            var configuration = Helpers.LoadConfiguration(context, log);
            string communicationsEndpointString = configuration[Constants.CommunicationsEndpointKey];            

            AccessToken accessToken;
            var communicationsEndpoint = new Uri(communicationsEndpointString);
            var communicationsCredential = new ManagedIdentityCredential();
            CommunicationIdentityClient client = new CommunicationIdentityClient(communicationsEndpoint, communicationsCredential);
            GetTokenForTeamsUserOptions options = new GetTokenForTeamsUserOptions(tokenRequest.token, tokenRequest.appId, tokenRequest.userId);

            try
            {
                accessToken = client.GetTokenForTeamsUser(options);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Exchanging ACS token from '{communicationsEndpoint}' failed. Exception: {ex}");
                return new Microsoft.AspNetCore.Mvc.NotFoundResult();
            }            
            
            string responseMessage = Helpers.SerializeWithCamelCasing(new AccessTokenResponse() 
            {
                token = accessToken.Token,
                expiresOn = accessToken.ExpiresOn
            });
            
            return new OkObjectResult(responseMessage);
        }

        private static async Task<ExchangeAccessTokenRequest> ExtractTokenFromContent(HttpRequest request, ILogger log)
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

            ExchangeAccessTokenRequest tokenRequest = null;
            if (requestBody != null)
            {
                try
                {
                    tokenRequest = JsonConvert.DeserializeObject<ExchangeAccessTokenRequest>(requestBody);
                }
                catch (Exception ex)
                {
                    tokenRequest = null;
                    log.LogInformation($"Parsing request json failed.\r\nJson:\r\n{requestBody}\r\nException:\r\n{ex}");
                }
            }

            if (tokenRequest != null)
            {
                if (string.IsNullOrWhiteSpace(tokenRequest.token))
                {
                    tokenRequest = null;
                    log.LogInformation($"Teams token empty.");
                }
            }

            return tokenRequest;
        }
    }
}
